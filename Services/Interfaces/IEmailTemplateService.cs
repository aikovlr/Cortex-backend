using Cortex.DTOs;

namespace Cortex.Services.Interfaces
{
    public interface IEmailTemplateService
    {
        EmailMensagem GerarCodigoVerificacao(string destinatario, string? nome, string codigo, int expiraEmMinutos = 15);

        EmailMensagem GerarCodigoRedefinicaoSenha(string destinatario, string? nome, string codigo, int expiraEmMinutos = 15);

        EmailMensagem GerarConfirmacaoSenhaAlterada(string destinatario, string? nome);

        EmailMensagem GerarNotificacaoEvento(string destinatario, string? nome, string tipo, string titulo, string mensagem, string? link = null);
    }
}

