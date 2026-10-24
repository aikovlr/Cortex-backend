using Cortex.DTOs.Request;
using Cortex.DTOs.Response;
using Cortex.Entities;
using Cortex.Infra.Exceptions;
using Cortex.Repositories.Interface;
using Cortex.Services.Interfaces;

namespace Cortex.Services.Implementations
{
    public class ResponsavelTarefaService : IResponsavelTarefaService
    {
        private readonly IResponsavelTarefaRepository _responsavelTarefaRepository;
        private readonly ITarefaRepository _tarefaRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IEquipeRepository _equipeRepository;
        private readonly IMembroEquipeRepository _membroEquipeRepository;
        private readonly ILogger<ResponsavelTarefaService> _logger;

        public ResponsavelTarefaService(
            IResponsavelTarefaRepository responsavelTarefaRepository,
            ITarefaRepository tarefaRepository,
            IUsuarioRepository usuarioRepository,
            IEquipeRepository equipeRepository,
            IMembroEquipeRepository membroEquipeRepository,
            ILogger<ResponsavelTarefaService> logger)
        {
            _responsavelTarefaRepository = responsavelTarefaRepository;
            _tarefaRepository = tarefaRepository;
            _usuarioRepository = usuarioRepository;
            _equipeRepository = equipeRepository;
            _membroEquipeRepository = membroEquipeRepository;
            _logger = logger;
        }

        public async Task<ConsultaResponsavelTarefaDTO> AtribuirResponsavelAsync(CriarResponsavelTarefaDTO dto, int solicitanteId)
        {
            var tarefa = await _tarefaRepository.ObterTarefaPorIdAsync(dto.TarefaId);
            if (tarefa == null)
                throw new KeyNotFoundException("Tarefa não encontrada.");

            var usuarioExiste = await _usuarioRepository.UsuarioExisteAsync(dto.UsuarioId);
            if (!usuarioExiste)
                throw new KeyNotFoundException("Usuário informado não foi encontrado.");

            // Permissão: Criador ou Dono/Admin
            await ValidarPermissaoGestaoTarefaAsync(tarefa, solicitanteId);

            var jaAtribuido = await _responsavelTarefaRepository.ResponsavelJaAtribuidoAsync(dto.TarefaId, dto.UsuarioId);
            if (jaAtribuido)
            {
                throw new InvalidOperationException("O usuário já está atribuído como responsável por esta tarefa.");
            }

            var novoResponsavel = new ResponsavelTarefa
            {
                TarefaId = dto.TarefaId,
                UsuarioId = dto.UsuarioId
            };

            await _responsavelTarefaRepository.AdicionarResponsavelTarefaAsync(novoResponsavel);

            _logger.LogInformation("Responsável atribuído: UsuarioId {UsuarioId} à TarefaId {TarefaId} por SolicitanteId {SolicitanteId}",
                dto.UsuarioId, dto.TarefaId, solicitanteId);

            var responsavelCompleto = await _responsavelTarefaRepository.ObterResponsavelTarefaPorIdAsync(novoResponsavel.Id);
            return MapToConsultaDTO(responsavelCompleto ?? novoResponsavel);
        }

        public async Task<IEnumerable<ConsultaResponsavelTarefaDTO>> AtribuirEquipeAsync(int tarefaId, int equipeId, int solicitanteId)
        {
            var tarefa = await _tarefaRepository.ObterTarefaPorIdAsync(tarefaId);
            if (tarefa == null)
                throw new KeyNotFoundException("Tarefa não encontrada.");

            var equipeExiste = await _equipeRepository.EquipeExisteAsync(equipeId);
            if (!equipeExiste)
                throw new KeyNotFoundException("Equipe informada não foi encontrada.");

            await ValidarPermissaoGestaoTarefaAsync(tarefa, solicitanteId);

            var membros = await _membroEquipeRepository.ObterMembrosEquipePorEquipeIdAsync(equipeId);
            var novosResponsaveis = new List<ResponsavelTarefa>();

            foreach (var membro in membros)
            {
                var jaAtribuido = await _responsavelTarefaRepository.ResponsavelJaAtribuidoAsync(tarefaId, membro.UsuarioId);
                if (!jaAtribuido)
                {
                    novosResponsaveis.Add(new ResponsavelTarefa
                    {
                        TarefaId = tarefaId,
                        UsuarioId = membro.UsuarioId
                    });
                }
            }

            if (novosResponsaveis.Count > 0)
            {
                await _responsavelTarefaRepository.AdicionarResponsaveisEmLoteAsync(novosResponsaveis);
                _logger.LogInformation("Atribuição em lote: {Total} membros da EquipeId {EquipeId} atribuídos à TarefaId {TarefaId}",
                    novosResponsaveis.Count, equipeId, tarefaId);
            }

            return await ListarResponsaveisPorTarefaAsync(tarefaId);
        }

        public async Task<bool> RemoverResponsavelAsync(int responsavelId, int solicitanteId)
        {
            var responsavel = await _responsavelTarefaRepository.ObterResponsavelTarefaPorIdAsync(responsavelId);
            if (responsavel == null)
                throw new KeyNotFoundException("Atribuição de responsabilidade não encontrada.");

            var ehProprioUsuario = responsavel.UsuarioId == solicitanteId;
            var ehCriador = responsavel.Tarefa?.CriadorId == solicitanteId;

            var usuario = await _usuarioRepository.ObterUsuarioPorIdAsync(solicitanteId);
            var ehAdminOuDono = usuario != null && usuario.Equipes.Any(e => e.Cargo == Cargo.Dono || e.Cargo == Cargo.Admin);

            if (!ehProprioUsuario && !ehCriador && !ehAdminOuDono)
            {
                throw new ForbiddenException("Você não possui permissão para desvincular este responsável.");
            }

            var removido = await _responsavelTarefaRepository.RemoverResponsavelTarefaAsync(responsavelId);

            if (removido)
            {
                _logger.LogInformation("Responsabilidade {ResponsavelId} removida da TarefaId {TarefaId} por SolicitanteId {SolicitanteId}",
                    responsavelId, responsavel.TarefaId, solicitanteId);
            }

            return removido;
        }

        public async Task<IEnumerable<ConsultaResponsavelTarefaDTO>> ListarResponsaveisPorTarefaAsync(int tarefaId)
        {
            var responsaveis = await _responsavelTarefaRepository.ObterResponsaveisPorTarefaIdAsync(tarefaId);
            return responsaveis.Select(MapToConsultaDTO);
        }

        private async Task ValidarPermissaoGestaoTarefaAsync(Tarefa tarefa, int solicitanteId)
        {
            if (tarefa.CriadorId == solicitanteId)
                return;

            var usuario = await _usuarioRepository.ObterUsuarioPorIdAsync(solicitanteId);
            var ehAdminOuDono = usuario != null && usuario.Equipes.Any(e => e.Cargo == Cargo.Dono || e.Cargo == Cargo.Admin);

            if (!ehAdminOuDono)
            {
                throw new ForbiddenException("Apenas o criador da tarefa ou administradores possuem permissão para gerenciar responsáveis.");
            }
        }

        private static ConsultaResponsavelTarefaDTO MapToConsultaDTO(ResponsavelTarefa responsavel)
        {
            return new ConsultaResponsavelTarefaDTO
            {
                Id = responsavel.Id,
                TarefaId = responsavel.TarefaId,
                UsuarioId = responsavel.UsuarioId,
                Usuario = responsavel.Usuario != null ? new ConsultaUsuarioDTO
                {
                    Id = responsavel.Usuario.Id,
                    Nome = responsavel.Usuario.Nome,
                    Email = responsavel.Usuario.Email,
                    Telefone = responsavel.Usuario.Telefone,
                    FotoPerfil = responsavel.Usuario.FotoPerfil != null ? new ConsultaAnexoDTO
                    {
                        Id = responsavel.Usuario.FotoPerfil.Id,
                        UrlCaminho = responsavel.Usuario.FotoPerfil.UrlCaminho,
                        NomeOriginal = responsavel.Usuario.FotoPerfil.NomeOriginal,
                        MimeType = responsavel.Usuario.FotoPerfil.MimeType,
                        DataEnvio = responsavel.Usuario.FotoPerfil.DataEnvio
                    } : null
                } : null
            };
        }
    }
}
