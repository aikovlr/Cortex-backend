using Cortex.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Cortex.Services.Implementations
{
    public class ArquivoValidacaoService : IArquivoValidacaoService
    {
        // Magic bytes para validação real do conteúdo (não confia apenas na extensão)
        private static readonly Dictionary<string, byte[][]> MagicBytes = new()
        {
            { ".jpg",  [new byte[] { 0xFF, 0xD8, 0xFF }] },
            { ".jpeg", [new byte[] { 0xFF, 0xD8, 0xFF }] },
            { ".png",  [new byte[] { 0x89, 0x50, 0x4E, 0x47 }] },
            { ".webp", [new byte[] { 0x52, 0x49, 0x46, 0x46 }] },
            { ".pdf",  [new byte[] { 0x25, 0x50, 0x44, 0x46 }] },
            { ".docx", [new byte[] { 0x50, 0x4B, 0x03, 0x04 }] },
            { ".xlsx", [new byte[] { 0x50, 0x4B, 0x03, 0x04 }] },
        };

        private static readonly string[] ExtensoesFotoPermitidas = [".jpg", ".jpeg", ".png", ".webp"];
        private static readonly string[] ExtensoesAnexoPermitidas = [".jpg", ".jpeg", ".png", ".webp", ".pdf", ".docx", ".xlsx", ".txt"];
        private const long TamanhoMaximoFoto = 5 * 1024 * 1024;   // 5 MB
        private const long TamanhoMaximoAnexo = 20 * 1024 * 1024; // 20 MB

        public void ValidarImagemPerfil(IFormFile arquivo)
            => ValidarArquivo(arquivo, ExtensoesFotoPermitidas, TamanhoMaximoFoto);

        public void ValidarAnexoTarefa(IFormFile arquivo)
            => ValidarArquivo(arquivo, ExtensoesAnexoPermitidas, TamanhoMaximoAnexo);

        public void ValidarArquivo(IFormFile arquivo, string[] extensoesPermitidas, long tamanhoMaximoBytes)
        {
            if (arquivo == null || arquivo.Length == 0)
                throw new ArgumentException("O arquivo é inválido ou está vazio.");

            if (arquivo.Length > tamanhoMaximoBytes)
                throw new ArgumentException($"O arquivo excede o tamanho máximo permitido de {tamanhoMaximoBytes / 1024 / 1024}MB.");

            // Sanitiza o nome original — impede path traversal
            var nomeOriginalSanitizado = Path.GetFileName(arquivo.FileName);
            var extensao = Path.GetExtension(nomeOriginalSanitizado).ToLowerInvariant();

            if (!extensoesPermitidas.Contains(extensao))
                throw new ArgumentException($"Tipo de arquivo não permitido. Extensões aceitas: {string.Join(", ", extensoesPermitidas)}");

            // Valida magic bytes (conteúdo real do arquivo)
            if (MagicBytes.TryGetValue(extensao, out var assinaturasValidas))
            {
                using var stream = arquivo.OpenReadStream();
                var header = new byte[8];
                var bytesLidos = stream.Read(header, 0, header.Length);

                var assinaturaValida = assinaturasValidas.Any(assinatura =>
                    bytesLidos >= assinatura.Length &&
                    header.Take(assinatura.Length).SequenceEqual(assinatura));

                if (!assinaturaValida)
                    throw new ArgumentException("O conteúdo do arquivo não corresponde à extensão informada.");
            }
        }
    }
}
