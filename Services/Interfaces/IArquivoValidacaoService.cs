using Microsoft.AspNetCore.Http;

namespace Cortex.Services.Interfaces
{
    public interface IArquivoValidacaoService
    {
        /// <summary>Valida extensão, MIME type e tamanho do arquivo. Lança ArgumentException se inválido.</summary>
        void ValidarArquivo(IFormFile arquivo, string[] extensoesPermitidas, long tamanhoMaximoBytes);

        /// <summary>Validação específica para imagens de perfil.</summary>
        void ValidarImagemPerfil(IFormFile arquivo);

        /// <summary>Validação genérica para anexos de tarefas.</summary>
        void ValidarAnexoTarefa(IFormFile arquivo);
    }
}
