using Cortex.DTOs.Response;
using Cortex.Entities;
using Cortex.Infra.Exceptions;
using Cortex.Repositories.Interface;
using Cortex.Services.Interfaces;

namespace Cortex.Services.Implementations
{
    public class AnexoService : IAnexoService
    {
        private readonly IAnexoRepository _anexoRepository;
        private readonly ITarefaRepository _tarefaRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IArquivoValidacaoService _arquivoValidacaoService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AnexoService> _logger;

        public AnexoService(
            IAnexoRepository anexoRepository,
            ITarefaRepository tarefaRepository,
            IUsuarioRepository usuarioRepository,
            IArquivoValidacaoService arquivoValidacaoService,
            IWebHostEnvironment environment,
            ILogger<AnexoService> logger)
        {
            _anexoRepository = anexoRepository;
            _tarefaRepository = tarefaRepository;
            _usuarioRepository = usuarioRepository;
            _arquivoValidacaoService = arquivoValidacaoService;
            _environment = environment;
            _logger = logger;
        }

        public async Task<ConsultaAnexoDTO> AdicionarAnexoTarefaAsync(int tarefaId, IFormFile arquivo, int solicitanteId)
        {
            var tarefa = await _tarefaRepository.ObterTarefaPorIdAsync(tarefaId);
            if (tarefa == null)
                throw new KeyNotFoundException("Tarefa não encontrada.");

            await ValidarAcessoTarefaAsync(tarefa, solicitanteId);

            if (arquivo == null || arquivo.Length == 0)
                throw new ArgumentException("O arquivo de anexo é inválido ou está vazio.");

            var anexo = await SalvarArquivoFisicoAsync(arquivo, "tarefas", TipoEntidade.Tarefa, tarefaId, null, solicitanteId);

            await _anexoRepository.AdicionarAnexoAsync(anexo);

            _logger.LogInformation("Anexo {AnexoId} ('{NomeOriginal}') adicionado à TarefaId {TarefaId} por UsuarioId {UsuarioId}",
                anexo.Id, anexo.NomeOriginal, tarefaId, solicitanteId);

            return MapToConsultaDTO(anexo);
        }

        public async Task<IEnumerable<ConsultaAnexoDTO>> AdicionarAnexosTarefaAsync(int tarefaId, List<IFormFile> arquivos, int solicitanteId)
        {
            var tarefa = await _tarefaRepository.ObterTarefaPorIdAsync(tarefaId);
            if (tarefa == null)
                throw new KeyNotFoundException("Tarefa não encontrada.");

            await ValidarAcessoTarefaAsync(tarefa, solicitanteId);

            var resultado = new List<ConsultaAnexoDTO>();

            foreach (var arquivo in arquivos)
            {
                if (arquivo.Length == 0) continue;

                var anexo = await SalvarArquivoFisicoAsync(arquivo, "tarefas", TipoEntidade.Tarefa, tarefaId, null, solicitanteId);
                await _anexoRepository.AdicionarAnexoAsync(anexo);
                resultado.Add(MapToConsultaDTO(anexo));
            }

            _logger.LogInformation("{Qtd} anexos adicionados à TarefaId {TarefaId} por UsuarioId {UsuarioId}",
                resultado.Count, tarefaId, solicitanteId);

            return resultado;
        }

        public async Task<ConsultaAnexoDTO?> ObterAnexoPorIdAsync(int id, int solicitanteId)
        {
            var anexo = await _anexoRepository.ObterAnexoPorIdAsync(id);
            if (anexo == null)
                return null;

            if (anexo.Tarefa != null)
            {
                await ValidarAcessoTarefaAsync(anexo.Tarefa, solicitanteId);
            }

            return MapToConsultaDTO(anexo);
        }

        public async Task<IEnumerable<ConsultaAnexoDTO>> ListarAnexosPorTarefaAsync(int tarefaId, int solicitanteId)
        {
            var tarefa = await _tarefaRepository.ObterTarefaPorIdAsync(tarefaId);
            if (tarefa == null)
                throw new KeyNotFoundException("Tarefa não encontrada.");

            var anexos = await _anexoRepository.ObterAnexosPorTarefaIdAsync(tarefaId);
            return anexos.Select(MapToConsultaDTO);
        }

        public async Task<IEnumerable<ConsultaAnexoDTO>> ListarAnexosPorEntidadeAsync(TipoEntidade tipoEntidade, int entidadeId, int solicitanteId)
        {
            var anexos = await _anexoRepository.ObterAnexosPorEntidadeAsync(tipoEntidade, entidadeId);
            return anexos.Select(MapToConsultaDTO);
        }

        public async Task<bool> DeletarAnexoAsync(int id, int solicitanteId)
        {
            var anexo = await _anexoRepository.ObterAnexoPorIdAsync(id);
            if (anexo == null)
                return false;

            var solicitante = await _usuarioRepository.ObterUsuarioPorIdAsync(solicitanteId);
            var ehAdminOuDono = solicitante != null && solicitante.Equipes.Any(e => e.Cargo == Cargo.Dono || e.Cargo == Cargo.Admin);

            // Permissão: uploader do anexo, criador da tarefa ou Admin/Dono
            var ehCriadorTarefa = anexo.Tarefa != null && anexo.Tarefa.CriadorId == solicitanteId;
            var ehUploader = anexo.UsuarioId == solicitanteId;

            if (!ehUploader && !ehCriadorTarefa && !ehAdminOuDono)
            {
                throw new ForbiddenException("Você não possui permissão para excluir este anexo.");
            }

            // Exclui arquivo físico
            var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var caminhoArquivo = Path.Combine(webRoot, anexo.UrlCaminho.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(caminhoArquivo))
            {
                try { File.Delete(caminhoArquivo); } catch { /* Ignore file lock */ }
            }

            var removido = await _anexoRepository.RemoverAnexoAsync(id);
            if (removido)
            {
                _logger.LogWarning("Auditoria: Exclusão de registro. Entidade: {Entidade}, Id: {Id}, DeletadoPor: {DeletadoPor}",
                    "Anexo", id, solicitanteId);
            }

            return removido;
        }

        private async Task ValidarAcessoTarefaAsync(Tarefa tarefa, int solicitanteId)
        {
            var ehCriador = tarefa.CriadorId == solicitanteId;
            var ehResponsavel = tarefa.ResponsaveisTarefas.Any(r => r.UsuarioId == solicitanteId);

            var solicitante = await _usuarioRepository.ObterUsuarioPorIdAsync(solicitanteId);
            var ehAdminOuDono = solicitante != null && solicitante.Equipes.Any(e => e.Cargo == Cargo.Dono || e.Cargo == Cargo.Admin);

            if (!ehCriador && !ehResponsavel && !ehAdminOuDono)
            {
                throw new ForbiddenException("Apenas o criador, responsáveis ou administradores podem gerenciar anexos da tarefa.");
            }
        }

        private async Task<Anexo> SalvarArquivoFisicoAsync(
            IFormFile arquivo, 
            string subPasta, 
            TipoEntidade tipoEntidade, 
            int? tarefaId, 
            int? respostaTarefaId, 
            int usuarioId)
        {
            _arquivoValidacaoService.ValidarAnexoTarefa(arquivo);

            var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadDir = Path.Combine(webRoot, "uploads", subPasta);
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
                UrlCaminho = $"/uploads/{subPasta}/{nomeArquivo}",
                TipoEntidade = tipoEntidade,
                DataEnvio = DateTime.UtcNow,
                TarefaId = tarefaId,
                RespostaTarefaId = respostaTarefaId,
                UsuarioId = usuarioId
            };
        }

        private static ConsultaAnexoDTO MapToConsultaDTO(Anexo anexo)
        {
            return new ConsultaAnexoDTO
            {
                Id = anexo.Id,
                UrlCaminho = anexo.UrlCaminho,
                NomeOriginal = anexo.NomeOriginal,
                MimeType = anexo.MimeType,
                DataEnvio = anexo.DataEnvio,
                TipoEntidade = anexo.TipoEntidade,
                TarefaId = anexo.TarefaId,
                RespostaTarefaId = anexo.RespostaTarefaId,
                UsuarioId = anexo.UsuarioId
            };
        }
    }
}
