using System.Net;
using System.Text;
using Cortex.DTOs;
using Cortex.Services.Interfaces;

namespace Cortex.Services.Implementations
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly string _frontendBaseUrl;

        public EmailTemplateService(IConfiguration configuration)
        {
            _frontendBaseUrl = configuration["App:FrontendBaseUrl"]?.TrimEnd('/') ?? "http://localhost:3000";
        }

        public EmailMensagem GerarCodigoVerificacao(string destinatario, string? nome, string codigo, int expiraEmMinutos = 15)
        {
            var nomeSeguro = WebUtility.HtmlEncode(nome ?? "Usuário");
            var codigoSeguro = WebUtility.HtmlEncode(codigo);
            var assunto = "Código de Verificação de Conta — Cortex";

            var htmlContent = $@"
                <p style=""font-size: 16px; margin-bottom: 20px;"">Olá, <strong>{nomeSeguro}</strong>!</p>
                <p style=""font-size: 15px; line-height: 1.6; color: #CBD5E1;"">
                    Obrigado por criar sua conta no <strong>Cortex</strong>. Para confirmar seu e-mail e ativar todas as notificações e recursos do sistema, utilize o código de segurança abaixo:
                </p>
                <div style=""margin: 30px 0; text-align: center;"">
                    <div style=""display: inline-block; background-color: #0F172A; border: 2px dashed #7B4FC9; border-radius: 8px; padding: 16px 32px; font-size: 32px; font-weight: bold; letter-spacing: 8px; color: #FFFFFF; font-family: 'Courier New', Courier, monospace;"">
                        {codigoSeguro}
                    </div>
                </div>
                <p style=""font-size: 13px; color: #94A3B8; text-align: center; margin-top: 10px;"">
                    Este código é de uso único e expira em <strong>{expiraEmMinutos} minutos</strong>.
                </p>
                <p style=""font-size: 13px; color: #94A3B8; margin-top: 25px; border-top: 1px solid #23304E; padding-top: 15px;"">
                    Se você não solicitou este cadastro no Cortex, por favor ignore este e-mail com segurança.
                </p>";

            var texto = $@"Olá, {nome ?? "Usuário"}!

Obrigado por se cadastrar no Cortex.
Seu código de verificação é: {codigo}

Ele é de uso único e expira em {expiraEmMinutos} minutos.

Se você não solicitou este cadastro, ignore esta mensagem.";

            return new EmailMensagem
            {
                Destinatario = destinatario,
                NomeDestinatario = nome,
                Assunto = assunto,
                CorpoHtml = MontarLayoutBase("Verificação de Conta", htmlContent),
                CorpoTexto = texto
            };
        }

        public EmailMensagem GerarCodigoRedefinicaoSenha(string destinatario, string? nome, string codigo, int expiraEmMinutos = 15)
        {
            var nomeSeguro = WebUtility.HtmlEncode(nome ?? "Usuário");
            var codigoSeguro = WebUtility.HtmlEncode(codigo);
            var assunto = "Redefinição de Senha — Cortex";

            var htmlContent = $@"
                <p style=""font-size: 16px; margin-bottom: 20px;"">Olá, <strong>{nomeSeguro}</strong>!</p>
                <p style=""font-size: 15px; line-height: 1.6; color: #CBD5E1;"">
                    Recebemos uma solicitação para redefinir a senha da sua conta no <strong>Cortex</strong>. Use o código de 6 dígitos a seguir para cadastrar sua nova senha:
                </p>
                <div style=""margin: 30px 0; text-align: center;"">
                    <div style=""display: inline-block; background-color: #0F172A; border: 2px solid #7B4FC9; border-radius: 8px; padding: 16px 32px; font-size: 32px; font-weight: bold; letter-spacing: 8px; color: #FFFFFF; font-family: 'Courier New', Courier, monospace;"">
                        {codigoSeguro}
                    </div>
                </div>
                <p style=""font-size: 13px; color: #94A3B8; text-align: center; margin-top: 10px;"">
                    Válido por <strong>{expiraEmMinutos} minutos</strong>. Limite de 5 tentativas.
                </p>
                <div style=""margin-top: 25px; background-color: #1E293B; border-left: 4px solid #EAB308; padding: 12px 16px; border-radius: 4px;"">
                    <p style=""font-size: 13px; color: #FDE047; margin: 0; line-height: 1.5;"">
                        <strong>Aviso de Segurança:</strong> Se você não solicitou a redefinição de senha, ignore este e-mail. Sua senha atual permanecerá segura.
                    </p>
                </div>";

            var texto = $@"Olá, {nome ?? "Usuário"}!

Recebemos uma solicitação para redefinir a senha da sua conta no Cortex.
Seu código de redefinição é: {codigo}

Ele é válido por {expiraEmMinutos} minutos (uso único).

Se você não solicitou a redefinição de senha, ignore esta mensagem.";

            return new EmailMensagem
            {
                Destinatario = destinatario,
                NomeDestinatario = nome,
                Assunto = assunto,
                CorpoHtml = MontarLayoutBase("Recuperação de Senha", htmlContent),
                CorpoTexto = texto
            };
        }

        public EmailMensagem GerarConfirmacaoSenhaAlterada(string destinatario, string? nome)
        {
            var nomeSeguro = WebUtility.HtmlEncode(nome ?? "Usuário");
            var dataFormatada = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm 'UTC'");
            var assunto = "Sua senha foi alterada com sucesso — Cortex";

            var htmlContent = $@"
                <p style=""font-size: 16px; margin-bottom: 20px;"">Olá, <strong>{nomeSeguro}</strong>!</p>
                <div style=""background-color: #064E3B; border-left: 4px solid #10B981; padding: 14px 18px; border-radius: 4px; margin-bottom: 20px;"">
                    <p style=""font-size: 14px; color: #A7F3D0; margin: 0; font-weight: 500;"">
                        ✓ A senha da sua conta Cortex foi alterada com sucesso em {dataFormatada}.
                    </p>
                </div>
                <p style=""font-size: 14px; line-height: 1.6; color: #CBD5E1;"">
                    Todas as sessões anteriores foram encerradas por motivos de segurança. Se você realizou essa alteração, nenhuma ação adicional é necessária.
                </p>
                <div style=""margin-top: 25px; background-color: #450A0A; border-left: 4px solid #EF4444; padding: 14px 18px; border-radius: 4px;"">
                    <p style=""font-size: 13px; color: #FECACA; margin: 0; line-height: 1.5;"">
                        <strong>Não reconhece esta ação?</strong> Caso você não tenha solicitado essa alteração, entre em contato imediatamente com o suporte técnico para proteger sua conta.
                    </p>
                </div>";

            var texto = $@"Olá, {nome ?? "Usuário"}!

A senha da sua conta Cortex foi alterada com sucesso em {dataFormatada}.
Todas as sessões antigas foram desconectadas.

Se você não reconhece esta alteração, entre em contato com o suporte imediatamente.";

            return new EmailMensagem
            {
                Destinatario = destinatario,
                NomeDestinatario = nome,
                Assunto = assunto,
                CorpoHtml = MontarLayoutBase("Segurança da Conta", htmlContent),
                CorpoTexto = texto
            };
        }

        public EmailMensagem GerarNotificacaoEvento(string destinatario, string? nome, string tipo, string titulo, string mensagem, string? link = null)
        {
            var nomeSeguro = WebUtility.HtmlEncode(nome ?? "Usuário");
            var tituloSeguro = WebUtility.HtmlEncode(titulo);
            var mensagemSegura = WebUtility.HtmlEncode(mensagem);

            var urlCompleta = FormatarLink(link);
            var badgeCor = ObterBadgeCor(tipo);

            var botaoHtml = string.IsNullOrWhiteSpace(urlCompleta)
                ? string.Empty
                : $@"
                <div style=""margin: 30px 0 15px 0; text-align: center;"">
                    <a href=""{urlCompleta}"" style=""display: inline-block; background-color: #7B4FC9; color: #FFFFFF; text-decoration: none; font-size: 15px; font-weight: 600; padding: 12px 28px; border-radius: 6px; box-shadow: 0 4px 12px rgba(123, 79, 201, 0.4);"">
                        Acessar no Cortex
                    </a>
                </div>";

            var htmlContent = $@"
                <p style=""font-size: 16px; margin-bottom: 15px;"">Olá, <strong>{nomeSeguro}</strong>!</p>
                <div style=""margin-bottom: 20px;"">
                    <span style=""display: inline-block; background-color: {badgeCor.Bg}; color: {badgeCor.Text}; font-size: 12px; font-weight: 600; padding: 4px 10px; border-radius: 12px; text-transform: uppercase; letter-spacing: 0.5px;"">
                        {WebUtility.HtmlEncode(ObterNomeAmigavelTipo(tipo))}
                    </span>
                </div>
                <h2 style=""font-size: 18px; color: #FFFFFF; margin: 0 0 12px 0; font-weight: 600;"">{tituloSeguro}</h2>
                <div style=""background-color: #0F172A; border: 1px solid #23304E; border-radius: 8px; padding: 16px; color: #E2E8F0; font-size: 14px; line-height: 1.6;"">
                    {mensagemSegura}
                </div>
                {botaoHtml}
                <p style=""font-size: 12px; color: #64748B; text-align: center; margin-top: 25px;"">
                    Você está recebendo esta notificação de acordo com as preferências configuradas no seu perfil do Cortex.
                </p>";

            var texto = $@"Olá, {nome ?? "Usuário"}!

{titulo}
----------------------------------------
{mensagem}

{(string.IsNullOrWhiteSpace(urlCompleta) ? string.Empty : $"Acesse: {urlCompleta}")}

Você está recebendo esta mensagem de acordo com suas preferências no Cortex.";

            return new EmailMensagem
            {
                Destinatario = destinatario,
                NomeDestinatario = nome,
                Assunto = $"{titulo} — Cortex",
                CorpoHtml = MontarLayoutBase(tituloSeguro, htmlContent),
                CorpoTexto = texto
            };
        }

        private string FormatarLink(string? link)
        {
            if (string.IsNullOrWhiteSpace(link))
                return string.Empty;

            if (link.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                link.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return link;
            }

            return $"{_frontendBaseUrl}/{(link.StartsWith('/') ? link[1..] : link)}";
        }

        private static (string Bg, string Text) ObterBadgeCor(string tipo)
        {
            return tipo.ToLowerInvariant() switch
            {
                "tarefa_vencida" or "atrasada" => ("#7F1D1D", "#FCA5A5"),
                "tarefa_urgente" => ("#78350F", "#FDE047"),
                "meta" => ("#064E3B", "#6EE7B7"),
                "ticket" or "ticket_criador" or "resposta_ticket" => ("#1E3A8A", "#93C5FD"),
                "sugestao" => ("#312E81", "#C7D2FE"),
                "equipe" => ("#4C1D95", "#DDD6FE"),
                _ => ("#334155", "#E2E8F0")
            };
        }

        private static string ObterNomeAmigavelTipo(string tipo)
        {
            return tipo.ToLowerInvariant() switch
            {
                "tarefa" => "Tarefa",
                "tarefa_vencida" => "Vencimento / Atraso",
                "tarefa_urgente" => "Urgência",
                "meta" => "Meta Concluída",
                "ticket" or "ticket_criador" or "resposta_ticket" => "Ticket de Reporte",
                "sugestao" => "Sugestão",
                "equipe" => "Equipe",
                _ => "Notificação"
            };
        }

        private static string MontarLayoutBase(string preheader, string conteudoHtml)
        {
            return $@"<!DOCTYPE html>
<html lang=""pt-BR"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{preheader}</title>
    <style>
        body {{
            margin: 0;
            padding: 0;
            background-color: #12192C;
            font-family: 'Poppins', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
            color: #FFFFFF;
            -webkit-font-smoothing: antialiased;
        }}
        table {{
            border-collapse: collapse;
        }}
    </style>
</head>
<body style=""margin: 0; padding: 0; background-color: #12192C;"">
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color: #12192C; padding: 30px 10px;"">
        <tr>
            <td align=""center"">
                <!-- Card Principal -->
                <table role=""presentation"" width=""100%"" style=""max-width: 580px; background-color: #1A233A; border: 1px solid #2A3655; border-radius: 12px; overflow: hidden; box-shadow: 0 8px 24px rgba(0, 0, 0, 0.4);"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                    <!-- Cabeçalho -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #1E1B4B 0%, #311042 100%); padding: 28px 32px; border-bottom: 2px solid #7B4FC9; text-align: center;"">
                            <span style=""font-size: 26px; font-weight: 800; letter-spacing: 2px; color: #FFFFFF;"">
                                CORTEX<span style=""color: #7B4FC9;"">.</span>
                            </span>
                        </td>
                    </tr>
                    <!-- Conteúdo Central -->
                    <tr>
                        <td style=""padding: 32px 32px 24px 32px; color: #E2E8F0; font-size: 15px;"">
                            {conteudoHtml}
                        </td>
                    </tr>
                    <!-- Rodapé -->
                    <tr>
                        <td style=""background-color: #0F172A; padding: 20px 32px; text-align: center; border-top: 1px solid #23304E;"">
                            <p style=""font-size: 12px; color: #64748B; margin: 0 0 6px 0;"">
                                Cortex &bull; Gestão Inteligente de Tarefas e Equipes
                            </p>
                            <p style=""font-size: 11px; color: #475569; margin: 0;"">
                                Esta é uma mensagem automática. Por favor, não responda diretamente a este e-mail.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }
    }
}

