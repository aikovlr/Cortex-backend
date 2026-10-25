using Cortex.DTOs.Request;
using Cortex.DTOs.Response;
using Cortex.Entities;
using Cortex.Infra.Exceptions;
using Cortex.Repositories.Interface;
using Cortex.Services.Interfaces;

namespace Cortex.Services.Implementations
{
    public class EquipeService : IEquipeService
    {
        private readonly IEquipeRepository _equipeRepository;
        private readonly IMembroEquipeRepository _membroEquipeRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IEquipeAuthorizationService _authService;
        private readonly ILogger<EquipeService> _logger;

        public EquipeService(
            IEquipeRepository equipeRepository,
            IMembroEquipeRepository membroEquipeRepository,
            IUsuarioRepository usuarioRepository,
            IEquipeAuthorizationService authService,
            ILogger<EquipeService> logger)
        {
            _equipeRepository = equipeRepository;
            _membroEquipeRepository = membroEquipeRepository;
            _usuarioRepository = usuarioRepository;
            _authService = authService;
            _logger = logger;
        }

        public async Task<ConsultaEquipeDTO> CriarEquipeAsync(CriarEquipeDTO dto, int criadorId)
        {
            var criador = await _usuarioRepository.ObterUsuarioPorIdAsync(criadorId);
            if (criador == null)
                throw new KeyNotFoundException("Usuário criador não encontrado.");

            var equipe = new Equipe
            {
                Nome = dto.Nome,
                DataCriacao = DateTime.UtcNow
            };

            await _equipeRepository.AdicionarEquipeAsync(equipe);

            // Criador se torna automaticamente o Dono da equipe
            var membroDono = new MembroEquipe
            {
                EquipeId = equipe.Id,
                UsuarioId = criadorId,
                Cargo = Cargo.Dono,
                DataEntrada = DateTime.UtcNow
            };

            await _membroEquipeRepository.AdicionarMembroEquipeAsync(membroDono);

            var equipeCompleta = await _equipeRepository.ObterEquipePorIdAsync(equipe.Id);
            return MapToConsultaDTO(equipeCompleta ?? equipe);
        }

        public async Task<ConsultaEquipeDTO?> ObterEquipePorIdAsync(int id, int? solicitanteId = null)
        {
            var equipe = await _equipeRepository.ObterEquipePorIdAsync(id);
            if (equipe == null)
                return null;

            if (solicitanteId.HasValue)
            {
                var ehMembro = equipe.Membros.Any(m => m.UsuarioId == solicitanteId.Value);
                if (!ehMembro)
                    throw new ForbiddenException("Você não possui permissão para visualizar esta equipe.");
            }

            return MapToConsultaDTO(equipe);
        }

        public async Task<IEnumerable<ConsultaEquipeDTO>> ListarEquipesAsync(int? solicitanteId = null)
        {
            List<Equipe> equipes;

            if (solicitanteId.HasValue)
            {
                equipes = await _equipeRepository.ObterEquipesPorUsuarioIdAsync(solicitanteId.Value);
            }
            else
            {
                equipes = await _equipeRepository.ObterTodasEquipesAsync();
            }

            return equipes.Select(MapToConsultaDTO);
        }

        public async Task<ConsultaEquipeDTO> AtualizarEquipeAsync(int id, AtualizarEquipeDTO dto, int solicitanteId)
        {
            var equipe = await _equipeRepository.ObterEquipePorIdAsync(id);
            if (equipe == null)
                throw new KeyNotFoundException("Equipe não encontrada.");

            // Apenas Dono ou Admin da equipe podem atualizar dados da equipe
            await _authService.ExigirAdminOuDonoDaEquipeAsync(solicitanteId, id);

            equipe.Nome = dto.Nome;
            await _equipeRepository.AtualizarEquipeAsync(equipe);

            var equipeAtualizada = await _equipeRepository.ObterEquipePorIdAsync(id);
            return MapToConsultaDTO(equipeAtualizada ?? equipe);
        }

        public async Task<bool> DeletarEquipeAsync(int id, int solicitanteId)
        {
            var equipe = await _equipeRepository.ObterEquipePorIdAsync(id);
            if (equipe == null)
                return false;

            // Apenas o Dono da equipe pode excluí-la
            await _authService.ExigirDonoDaEquipeAsync(solicitanteId, id);

            var removida = await _equipeRepository.RemoverEquipeAsync(id);
            if (removida)
            {
                _logger.LogWarning("Auditoria: Exclusão de registro. Entidade: {Entidade}, Id: {Id}, DeletadoPor: {DeletadoPor}",
                    "Equipe", id, solicitanteId);
            }

            return removida;
        }

        private static ConsultaEquipeDTO MapToConsultaDTO(Equipe equipe)
        {
            return new ConsultaEquipeDTO
            {
                Id = equipe.Id,
                Nome = equipe.Nome,
                DataCriacao = equipe.DataCriacao,
                Membros = equipe.Membros.Select(m => new ConsultaMembroEquipeDTO
                {
                    Id = m.Id,
                    DataEntrada = m.DataEntrada,
                    Cargo = m.Cargo,
                    UsuarioId = m.UsuarioId,
                    EquipeId = m.EquipeId,
                    Usuario = m.Usuario != null ? new ConsultaUsuarioDTO
                    {
                        Id = m.Usuario.Id,
                        Nome = m.Usuario.Nome,
                        Email = m.Usuario.Email,
                        Telefone = m.Usuario.Telefone,
                        FotoPerfil = m.Usuario.FotoPerfil != null ? new ConsultaAnexoDTO
                        {
                            Id = m.Usuario.FotoPerfil.Id,
                            UrlCaminho = m.Usuario.FotoPerfil.UrlCaminho,
                            NomeOriginal = m.Usuario.FotoPerfil.NomeOriginal,
                            MimeType = m.Usuario.FotoPerfil.MimeType,
                            DataEnvio = m.Usuario.FotoPerfil.DataEnvio
                        } : null
                    } : null
                }).ToList()
            };
        }
    }
}
