using Cortex.DTOs.Request;
using Cortex.DTOs.Response;
using Cortex.Entities;
using Cortex.Infra.Exceptions;
using Cortex.Repositories.Interface;
using Cortex.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Cortex.Services.Implementations
{
    public class TarefaService : ITarefaService
    {
        private readonly ITarefaRepository _tarefaRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IMetaService _metaService;
        private readonly IEquipeAuthorizationService _authService;
        private readonly IArquivoValidacaoService _arquivoValidacaoService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<TarefaService> _logger;

        public TarefaService(
            ITarefaRepository tarefaRepository,
            IUsuarioRepository usuarioRepository,
            IMetaService metaService,
            IEquipeAuthorizationService authService,
            IArquivoValidacaoService arquivoValidacaoService,
            IWebHostEnvironment environment,
            ILogger<TarefaService> logger)
        {
            _tarefaRepository = tarefaRepository;
            _usuarioRepository = usuarioRepository;
            _metaService = metaService;
            _authService = authService;
            _arquivoValidacaoService = arquivoValidacaoService;
            _environment = environment;
            _logger = logger;
        }

        public async Task<ConsultaTarefaDTO> CriarTarefaAsync(CriarTarefaDTO dto, int criadorId)
        {
            var criador = await _usuarioRepository.ObterUsuarioPorIdAsync(criadorId);
            if (criador == null)
                throw new KeyNotFoundException("Usuário criador não encontrado.");

            var dataVencimentoUtc = dto.DataVencimento.ToUniversalTime();

            var tarefa = new Tarefa
            {
                Titulo = dto.Titulo,
                Descricao = dto.Descricao,
                DataVencimento = dataVencimentoUtc,
                Pontuacao = dto.Pontuacao,
                Status = Status.Pendente,
                Prioridade = dto.Prioridade,
                CriadorId = criadorId,
                Criador = criador
            };

            if (dto.Anexos != null && dto.Anexos.Count > 0)
            {
                foreach (var arquivo in dto.Anexos)
                {
                    var anexo = await SalvarAnexoFisicoAsync(arquivo);
                    tarefa.Anexos.Add(anexo);
                }
            }

            await _tarefaRepository.AdicionarTarefaAsync(tarefa);

            _logger.LogInformation("Auditoria: Tarefa criada. TarefaId: {TarefaId}, CriadorId: {CriadorId}, Titulo: {Titulo}",
                tarefa.Id, criadorId, tarefa.Titulo);

            return MapToConsultaDTO(tarefa);
        }

        public async Task<DetalhesTarefaDTO?> ObterTarefaPorIdAsync(int id, int? solicitanteId = null)
        {
            var tarefa = await _tarefaRepository.ObterTarefaPorIdAsync(id);
            if (tarefa == null)
                return null;

            return MapToDetalhesDTO(tarefa);
        }

        public async Task<IEnumerable<ConsultaTarefaDTO>> ListarTarefasAsync(FiltroTarefasDTO? filtros = null, int? solicitanteId = null)
        {
            var dataInicioUtc = filtros?.DataInicio?.ToUniversalTime();
            var dataFimUtc = filtros?.DataFim?.ToUniversalTime();

            var tarefas = await _tarefaRepository.ObterTarefasComFiltroAsync(
                status: filtros?.Status,
                prioridade: filtros?.Prioridade,
                responsavelId: filtros?.ResponsavelId,
                criadorId: filtros?.CriadorId,
                dataInicio: dataInicioUtc,
                dataFim: dataFimUtc,
                termoBusca: filtros?.TermoBusca);

            return tarefas.Select(MapToConsultaDTO);
        }

        public async Task<DetalhesTarefaDTO> AtualizarTarefaAsync(int id, AtualizarTarefaDTO dto, int solicitanteId)
        {
            var tarefa = await _tarefaRepository.ObterTarefaPorIdAsync(id);
            if (tarefa == null)
                throw new KeyNotFoundException("Tarefa não encontrada.");

            // Apenas o criador ou Dono/Admin da equipe possuem autorização para alterar a tarefa
            var ehCriador = tarefa.CriadorId == solicitanteId;
            var ehAdminOuDono = await _authService.EhAdminOuDonoDaTarefaAsync(solicitanteId, tarefa);

            if (!ehCriador && !ehAdminOuDono)
            {
                throw new ForbiddenException("Apenas o criador da tarefa ou administradores da equipe possuem permissão para editá-la.");
            }

            var camposAlterados = new List<string>();

            // Aplicar campos somente se enviados (patch semântico)
            if (!string.IsNullOrWhiteSpace(dto.Titulo) && tarefa.Titulo != dto.Titulo)
            {
                tarefa.Titulo = dto.Titulo;
                camposAlterados.Add("Titulo");
            }

            if (dto.Descricao != null && tarefa.Descricao != dto.Descricao) // null = não alterar; "" = limpar
            {
                tarefa.Descricao = dto.Descricao;
                camposAlterados.Add("Descricao");
            }

            if (dto.DataVencimento.HasValue)
            {
                var dataUtc = dto.DataVencimento.Value.Kind == DateTimeKind.Utc
                    ? dto.DataVencimento.Value
                    : dto.DataVencimento.Value.ToUniversalTime();

                if (tarefa.DataVencimento != dataUtc)
                {
                    tarefa.DataVencimento = dataUtc;
                    camposAlterados.Add("DataVencimento");
                }
            }

            if (dto.Pontuacao.HasValue && tarefa.Pontuacao != dto.Pontuacao.Value)
            {
                tarefa.Pontuacao = dto.Pontuacao.Value;
                camposAlterados.Add("Pontuacao");
            }

            if (dto.Prioridade.HasValue && tarefa.Prioridade != dto.Prioridade.Value)
            {
                tarefa.Prioridade = dto.Prioridade.Value;
                camposAlterados.Add("Prioridade");
            }

            if (dto.Anexos != null && dto.Anexos.Count > 0)
            {
                foreach (var arquivo in dto.Anexos)
                {
                    var anexo = await SalvarAnexoFisicoAsync(arquivo);
                    tarefa.Anexos.Add(anexo);
                }
                camposAlterados.Add("Anexos");
            }

            await _tarefaRepository.AtualizarTarefaAsync(tarefa);

            _logger.LogInformation("Auditoria: Tarefa alterada. TarefaId: {TarefaId}, AlteradoPor: {AlteradoPor}, CamposAlterados: {CamposAlterados}",
                id, solicitanteId, camposAlterados);

            var tarefaAtualizada = await _tarefaRepository.ObterTarefaPorIdAsync(id);
            return MapToDetalhesDTO(tarefaAtualizada ?? tarefa);
        }

        public async Task<bool> DeletarTarefaAsync(int id, int solicitanteId)
        {
            var tarefa = await _tarefaRepository.ObterTarefaPorIdAsync(id);
            if (tarefa == null)
                return false;

            var ehCriador = tarefa.CriadorId == solicitanteId;
            var ehAdminOuDono = await _authService.EhAdminOuDonoDaTarefaAsync(solicitanteId, tarefa);

            if (!ehCriador && !ehAdminOuDono)
            {
                throw new ForbiddenException("Apenas o criador da tarefa ou administradores da equipe possuem permissão para excluí-la.");
            }

            var removida = await _tarefaRepository.RemoverTarefaAsync(id);

            if (removida)
            {
                _logger.LogWarning("Auditoria: Exclusão de registro. Entidade: {Entidade}, Id: {Id}, DeletadoPor: {DeletadoPor}",
                    "Tarefa", id, solicitanteId);
            }

            return removida;
        }

        public async Task<DetalhesTarefaDTO> ConcluirTarefaAsync(int id, int solicitanteId)
        {
            var tarefa = await _tarefaRepository.ObterTarefaPorIdAsync(id);
            if (tarefa == null)
                throw new KeyNotFoundException("Tarefa não encontrada.");

            var ehCriador = tarefa.CriadorId == solicitanteId;
            var ehResponsavel = tarefa.ResponsaveisTarefas.Any(rt => rt.UsuarioId == solicitanteId);
            var ehAdminOuDono = await _authService.EhAdminOuDonoDaTarefaAsync(solicitanteId, tarefa);

            if (!ehCriador && !ehResponsavel && !ehAdminOuDono)
            {
                throw new ForbiddenException("Você não possui permissão para concluir esta tarefa.");
            }

            tarefa.Status = Status.Concluida;
            await _tarefaRepository.AtualizarTarefaAsync(tarefa);

            _logger.LogInformation("Auditoria: Tarefa concluída. TarefaId: {TarefaId}, ConcluidaPor: {ConcluidaPor}, Timestamp: {Timestamp}",
                id, solicitanteId, DateTime.UtcNow);

            // Delega a verificação de metas ao MetaService (SRP — cada serviço cuida do seu domínio)
            await _metaService.VerificarConclusaoMetaAsync(id);

            var tarefaAtualizada = await _tarefaRepository.ObterTarefaPorIdAsync(id);
            return MapToDetalhesDTO(tarefaAtualizada ?? tarefa);
        }

        public async Task<int> VerificarTarefasAtrasadasAsync()
        {
            // RN06: Tarefas cuja DataVencimento < UtcNow e Status == Pendente devem transicionar para Atrasada
            var tarefasVencidas = await _tarefaRepository.ObterTarefasVencidasNaoConcluidasAsync(DateTime.UtcNow);
            var totalAtrasadas = 0;

            foreach (var tarefa in tarefasVencidas)
            {
                tarefa.Status = Status.Atrasada;
                await _tarefaRepository.AtualizarTarefaAsync(tarefa);
                totalAtrasadas++;

                _logger.LogWarning("Tarefa {TarefaId} ('{Titulo}') transitou automaticamente para Atrasada. Vencimento era: {DataVencimento}",
                    tarefa.Id, tarefa.Titulo, tarefa.DataVencimento);
            }

            return totalAtrasadas;
        }

        private async Task<Anexo> SalvarAnexoFisicoAsync(IFormFile arquivo)
        {
            _arquivoValidacaoService.ValidarAnexoTarefa(arquivo);

            var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadDir = Path.Combine(webRoot, "uploads", "tarefas");
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            var nomeOriginalSanitizado = Path.GetFileName(arquivo.FileName);
            var extensao = Path.GetExtension(nomeOriginalSanitizado).ToLowerInvariant();
            var nomeArquivo = $"{Guid.NewGuid()}{extensao}";
            var caminhoCompleto = Path.Combine(uploadDir, nomeArquivo);

            using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
            {
                await arquivo.CopyToAsync(stream);
            }

            return new Anexo
            {
                NomeOriginal = arquivo.FileName,
                MimeType = arquivo.ContentType,
                UrlCaminho = $"/uploads/tarefas/{nomeArquivo}",
                TipoEntidade = TipoEntidade.Tarefa,
                DataEnvio = DateTime.UtcNow
            };
        }

        private static ConsultaTarefaDTO MapToConsultaDTO(Tarefa tarefa)
        {
            return new ConsultaTarefaDTO
            {
                Id = tarefa.Id,
                Titulo = tarefa.Titulo,
                Descricao = tarefa.Descricao,
                DataVencimento = tarefa.DataVencimento,
                Pontuacao = tarefa.Pontuacao,
                Status = tarefa.Status,
                Prioridade = tarefa.Prioridade,
                CriadorId = tarefa.CriadorId
            };
        }

        private static DetalhesTarefaDTO MapToDetalhesDTO(Tarefa tarefa)
        {
            return new DetalhesTarefaDTO
            {
                Id = tarefa.Id,
                Titulo = tarefa.Titulo,
                Descricao = tarefa.Descricao,
                DataVencimento = tarefa.DataVencimento,
                Pontuacao = tarefa.Pontuacao,
                Status = tarefa.Status,
                Prioridade = tarefa.Prioridade,
                CriadorId = tarefa.CriadorId,
                Criador = tarefa.Criador != null ? new ConsultaUsuarioDTO
                {
                    Id = tarefa.Criador.Id,
                    Nome = tarefa.Criador.Nome,
                    Email = tarefa.Criador.Email,
                    Telefone = tarefa.Criador.Telefone,
                    FotoPerfil = tarefa.Criador.FotoPerfil != null ? new ConsultaAnexoDTO
                    {
                        Id = tarefa.Criador.FotoPerfil.Id,
                        UrlCaminho = tarefa.Criador.FotoPerfil.UrlCaminho,
                        NomeOriginal = tarefa.Criador.FotoPerfil.NomeOriginal,
                        MimeType = tarefa.Criador.FotoPerfil.MimeType,
                        DataEnvio = tarefa.Criador.FotoPerfil.DataEnvio
                    } : null
                } : null,
                Responsaveis = tarefa.ResponsaveisTarefas.Select(rt => new ConsultaResponsavelTarefaDTO
                {
                    Id = rt.Id,
                    TarefaId = rt.TarefaId,
                    UsuarioId = rt.UsuarioId,
                    Usuario = rt.Usuario != null ? new ConsultaUsuarioDTO
                    {
                        Id = rt.Usuario.Id,
                        Nome = rt.Usuario.Nome,
                        Email = rt.Usuario.Email,
                        Telefone = rt.Usuario.Telefone,
                        FotoPerfil = rt.Usuario.FotoPerfil != null ? new ConsultaAnexoDTO
                        {
                            Id = rt.Usuario.FotoPerfil.Id,
                            UrlCaminho = rt.Usuario.FotoPerfil.UrlCaminho,
                            NomeOriginal = rt.Usuario.FotoPerfil.NomeOriginal,
                            MimeType = rt.Usuario.FotoPerfil.MimeType,
                            DataEnvio = rt.Usuario.FotoPerfil.DataEnvio
                        } : null
                    } : null
                }).ToList(),
                Anexos = tarefa.Anexos.Select(a => new ConsultaAnexoDTO
                {
                    Id = a.Id,
                    UrlCaminho = a.UrlCaminho,
                    NomeOriginal = a.NomeOriginal,
                    MimeType = a.MimeType,
                    DataEnvio = a.DataEnvio
                }).ToList(),
                Respostas = tarefa.Respostas.Select(r => new ConsultaRespostaTarefaDTO
                {
                    Id = r.Id,
                    DataResposta = r.DataResposta,
                    UsuarioId = r.UsuarioId,
                    TarefaId = r.TarefaId,
                    Anexos = r.Anexos.Select(ra => new ConsultaAnexoDTO
                    {
                        Id = ra.Id,
                        UrlCaminho = ra.UrlCaminho,
                        NomeOriginal = ra.NomeOriginal,
                        MimeType = ra.MimeType,
                        DataEnvio = ra.DataEnvio
                    }).ToList()
                }).ToList()
            };
        }
    }
}
