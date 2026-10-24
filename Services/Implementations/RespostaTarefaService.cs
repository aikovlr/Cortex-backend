using Cortex.DTOs.Request;
using Cortex.DTOs.Response;
using Cortex.Entities;
using Cortex.Infra.Exceptions;
using Cortex.Repositories.Interface;
using Cortex.Services.Interfaces;

namespace Cortex.Services.Implementations
{
    public class RespostaTarefaService : IRespostaTarefaService
    {
        private readonly IRespostaTarefaRepository _respostaRepository;
        private readonly ITarefaRepository _tarefaRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IAnexoRepository _anexoRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<RespostaTarefaService> _logger;

        public RespostaTarefaService(
            IRespostaTarefaRepository respostaRepository,
            ITarefaRepository tarefaRepository,
            IUsuarioRepository usuarioRepository,
            IAnexoRepository anexoRepository,
            IWebHostEnvironment environment,
            ILogger<RespostaTarefaService> logger)
        {
            _respostaRepository = respostaRepository;
            _tarefaRepository = tarefaRepository;
            _usuarioRepository = usuarioRepository;
            _anexoRepository = anexoRepository;
            _environment = environment;
            _logger = logger;
        }

        public async Task<ConsultaRespostaTarefaDTO> CriarRespostaAsync(CriarRespostaTarefaDTO dto, int solicitanteId)
        {
            var tarefa = await _tarefaRepository.ObterTarefaPorIdAsync(dto.TarefaId);
            if (tarefa == null)
            {
                throw new KeyNotFoundException("Tarefa vinculada não encontrada.");
            }

            var ehCriador = tarefa.CriadorId == solicitanteId;
            var ehResponsavel = tarefa.ResponsaveisTarefas.Any(r => r.UsuarioId == solicitanteId);

            var solicitante = await _usuarioRepository.ObterUsuarioPorIdAsync(solicitanteId);
            var ehAdminOuDono = solicitante != null && solicitante.Equipes.Any(e => e.Cargo == Cargo.Dono || e.Cargo == Cargo.Admin);

            if (!ehCriador && !ehResponsavel && !ehAdminOuDono)
            {
                throw new ForbiddenException("Apenas o criador, os responsáveis atribuídos ou administradores podem responder à tarefa.");
            }

            var resposta = new RespostaTarefa
            {
                TarefaId = dto.TarefaId,
                UsuarioId = solicitanteId,
                DataResposta = DateTime.UtcNow
            };

            await _respostaRepository.AdicionarRespostaTarefaAsync(resposta);

            // Upload de anexos caso existam
            if (dto.Anexos != null && dto.Anexos.Any())
            {
                var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadDir = Path.Combine(webRoot, "uploads", "respostas");
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                foreach (var arquivo in dto.Anexos)
                {
                    if (arquivo.Length == 0) continue;

                    var extensao = Path.GetExtension(arquivo.FileName);
                    var nomeArquivo = $"{Guid.NewGuid()}{extensao}";
                    var caminhoCompleto = Path.Combine(uploadDir, nomeArquivo);

                    using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
                    {
                        await arquivo.CopyToAsync(stream);
                    }

                    var anexo = new Anexo
                    {
                        NomeOriginal = arquivo.FileName,
                        MimeType = arquivo.ContentType,
                        UrlCaminho = $"/uploads/respostas/{nomeArquivo}",
                        TipoEntidade = TipoEntidade.Tarefa,
                        DataEnvio = DateTime.UtcNow,
                        TarefaId = dto.TarefaId,
                        RespostaTarefaId = resposta.Id,
                        UsuarioId = solicitanteId
                    };

                    await _anexoRepository.AdicionarAnexoAsync(anexo);
                    resposta.Anexos.Add(anexo);
                }
            }

            _logger.LogInformation("Resposta {RespostaId} criada para a TarefaId {TarefaId} por UsuarioId {UsuarioId}",
                resposta.Id, dto.TarefaId, solicitanteId);

            var respostaCarregada = await _respostaRepository.ObterRespostaTarefaPorIdAsync(resposta.Id);
            return MapToConsultaDTO(respostaCarregada ?? resposta);
        }

        public async Task<IEnumerable<ConsultaRespostaTarefaDTO>> ListarRespostasPorTarefaAsync(int tarefaId, int solicitanteId)
        {
            var tarefa = await _tarefaRepository.ObterTarefaPorIdAsync(tarefaId);
            if (tarefa == null)
            {
                throw new KeyNotFoundException("Tarefa não encontrada.");
            }

            var respostas = await _respostaRepository.ObterRespostasTarefaPorTarefaIdAsync(tarefaId);
            return respostas.Select(MapToConsultaDTO);
        }

        public async Task<bool> DeletarRespostaAsync(int respostaId, int solicitanteId)
        {
            var resposta = await _respostaRepository.ObterRespostaTarefaPorIdAsync(respostaId);
            if (resposta == null)
                return false;

            var solicitante = await _usuarioRepository.ObterUsuarioPorIdAsync(solicitanteId);
            var ehAdminOuDono = solicitante != null && solicitante.Equipes.Any(e => e.Cargo == Cargo.Dono || e.Cargo == Cargo.Admin);

            if (resposta.UsuarioId != solicitanteId && !ehAdminOuDono)
            {
                throw new ForbiddenException("Apenas o autor da resposta ou administradores podem excluí-la.");
            }

            // Exclui arquivos físicos dos anexos associados
            var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            foreach (var anexo in resposta.Anexos)
            {
                var caminhoArquivo = Path.Combine(webRoot, anexo.UrlCaminho.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(caminhoArquivo))
                {
                    try { File.Delete(caminhoArquivo); } catch { /* Ignore file lock */ }
                }
            }

            var removida = await _respostaRepository.RemoverRespostaTarefaAsync(respostaId);
            if (removida)
            {
                _logger.LogWarning("Auditoria: Exclusão de registro. Entidade: {Entidade}, Id: {Id}, DeletadoPor: {DeletadoPor}",
                    "RespostaTarefa", respostaId, solicitanteId);
            }

            return removida;
        }

        private static ConsultaRespostaTarefaDTO MapToConsultaDTO(RespostaTarefa resposta)
        {
            return new ConsultaRespostaTarefaDTO
            {
                Id = resposta.Id,
                DataResposta = resposta.DataResposta,
                UsuarioId = resposta.UsuarioId,
                TarefaId = resposta.TarefaId,
                Usuario = resposta.Usuario != null ? new ConsultaUsuarioDTO
                {
                    Id = resposta.Usuario.Id,
                    Nome = resposta.Usuario.Nome,
                    Email = resposta.Usuario.Email,
                    Telefone = resposta.Usuario.Telefone,
                    FotoPerfil = resposta.Usuario.FotoPerfil != null ? new ConsultaAnexoDTO
                    {
                        Id = resposta.Usuario.FotoPerfil.Id,
                        UrlCaminho = resposta.Usuario.FotoPerfil.UrlCaminho,
                        NomeOriginal = resposta.Usuario.FotoPerfil.NomeOriginal,
                        MimeType = resposta.Usuario.FotoPerfil.MimeType,
                        DataEnvio = resposta.Usuario.FotoPerfil.DataEnvio,
                        TipoEntidade = resposta.Usuario.FotoPerfil.TipoEntidade
                    } : null
                } : null,
                Anexos = resposta.Anexos.Select(a => new ConsultaAnexoDTO
                {
                    Id = a.Id,
                    UrlCaminho = a.UrlCaminho,
                    NomeOriginal = a.NomeOriginal,
                    MimeType = a.MimeType,
                    DataEnvio = a.DataEnvio,
                    TipoEntidade = a.TipoEntidade,
                    TarefaId = a.TarefaId,
                    RespostaTarefaId = a.RespostaTarefaId,
                    UsuarioId = a.UsuarioId
                }).ToList()
            };
        }
    }
}
