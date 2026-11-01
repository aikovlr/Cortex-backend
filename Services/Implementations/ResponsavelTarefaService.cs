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
        private readonly INotificacaoService _notificacaoService;
        private readonly ILogger<ResponsavelTarefaService> _logger;

        public ResponsavelTarefaService(
            IResponsavelTarefaRepository responsavelTarefaRepository,
            ITarefaRepository tarefaRepository,
            IUsuarioRepository usuarioRepository,
            IEquipeRepository equipeRepository,
            IMembroEquipeRepository membroEquipeRepository,
            INotificacaoService notificacaoService,
            ILogger<ResponsavelTarefaService> logger)
        {
            _responsavelTarefaRepository = responsavelTarefaRepository;
            _tarefaRepository = tarefaRepository;
            _usuarioRepository = usuarioRepository;
            _equipeRepository = equipeRepository;
            _membroEquipeRepository = membroEquipeRepository;
            _notificacaoService = notificacaoService;
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

            var isUrgente = tarefa.Prioridade == Prioridade.Urgente;
            try
            {
                await _notificacaoService.CriarNotificacaoAsync(
                    dto.UsuarioId,
                    isUrgente ? "tarefa_urgente" : "tarefa",
                    isUrgente ? "Tarefa Urgente Atribuída" : "Nova Tarefa Atribuída",
                    isUrgente
                        ? $"Atenção: A tarefa \"{tarefa.Titulo}\" possui prioridade urgente e requer sua atenção imediata."
                        : $"Você foi designado como responsável pela tarefa \"{tarefa.Titulo}\".",
                    $"/minhas-tarefas/{tarefa.Id}",
                    destaque: isUrgente,
                    tarefaId: tarefa.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar notificação de atribuição de tarefa para UsuarioId {UsuarioId}", dto.UsuarioId);
            }

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

            var membroSolicitante = await _membroEquipeRepository.ObterMembroPorEquipeEUsuarioAsync(equipeId, solicitanteId);
            if (membroSolicitante == null || (membroSolicitante.Cargo != Cargo.Dono && membroSolicitante.Cargo != Cargo.Admin))
            {
                throw new ForbiddenException("Apenas o Dono ou Administradores da equipe possuem permissão para atribuir a equipe a uma tarefa.");
            }

            var membros = await _membroEquipeRepository.ObterMembrosEquipePorEquipeIdAsync(equipeId);
            var novosResponsaveis = new List<ResponsavelTarefa>();

            foreach (var membro in membros)
            {
                // O criador da tarefa não deve ser incluído na lista de responsáveis ao atribuir a uma equipe
                if (membro.UsuarioId == tarefa.CriadorId)
                    continue;

                // Hierarquia de cargo: Dono > Admin > Membro
                // O solicitante só pode atribuir tarefas a membros com cargo inferior na hierarquia
                if (ObterNivelCargo(membroSolicitante.Cargo) <= ObterNivelCargo(membro.Cargo))
                    continue;

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

                var isUrgenteEquipe = tarefa.Prioridade == Prioridade.Urgente;
                foreach (var resp in novosResponsaveis)
                {
                    try
                    {
                        await _notificacaoService.CriarNotificacaoAsync(
                            resp.UsuarioId,
                            isUrgenteEquipe ? "tarefa_urgente" : "tarefa",
                            isUrgenteEquipe ? "Tarefa Urgente Atribuída" : "Nova Tarefa Atribuída",
                            isUrgenteEquipe
                                ? $"Atenção: A tarefa \"{tarefa.Titulo}\" possui prioridade urgente e requer sua atenção imediata."
                                : $"Você foi designado como responsável pela tarefa \"{tarefa.Titulo}\".",
                            $"/minhas-tarefas/{tarefa.Id}",
                            destaque: isUrgenteEquipe,
                            tarefaId: tarefa.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao criar notificação de atribuição de tarefa para UsuarioId {UsuarioId}", resp.UsuarioId);
                    }
                }
            }

            _logger.LogInformation("Equipe {EquipeId} atribuída à TarefaId {TarefaId} com {Total} novos responsáveis.",
                equipeId, tarefaId, novosResponsaveis.Count);

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

        private static int ObterNivelCargo(Cargo cargo)
        {
            return cargo switch
            {
                Cargo.Dono => 3,
                Cargo.Admin => 2,
                Cargo.Membro => 1,
                _ => 0
            };
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
