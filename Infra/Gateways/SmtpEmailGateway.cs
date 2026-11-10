using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using Cortex.DTOs;
using Cortex.Infra.Configuration;
using Cortex.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Cortex.Infra.Gateways
{
    public class SmtpEmailGateway : IEmailGateway
    {
        private readonly SmtpOptions _options;
        private readonly ILogger<SmtpEmailGateway> _logger;
        private const int MaxTentativas = 1;

        // Controle de duplicidade: bloqueia se o mesmo disparo for acionado mais de 1 vez em 1 minuto
        private static readonly ConcurrentDictionary<string, DateTime> _ultimosDisparos = new();
        private static readonly TimeSpan JanelaBloqueioMesmoDisparo = TimeSpan.FromMinutes(1);

        public SmtpEmailGateway(
            IOptions<SmtpOptions> options,
            ILogger<SmtpEmailGateway> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<bool> EnviarEmailAsync(string destinatario, string assunto, string mensagemCorpo)
        {
            return await EnviarAsync(new EmailMensagem
            {
                Destinatario = destinatario,
                Assunto = assunto,
                CorpoHtml = $"<p>{mensagemCorpo}</p>",
                CorpoTexto = mensagemCorpo
            });
        }

        public async Task<bool> EnviarAsync(EmailMensagem mensagem, CancellationToken ct = default)
        {
            var emailMascarado = LogEmailGateway.MascararEmail(mensagem.Destinatario);
            var chaveDisparo = $"{mensagem.Destinatario?.Trim().ToLowerInvariant()}:{mensagem.Assunto?.Trim()}";
            var agoraUtc = DateTime.UtcNow;

            // Bloqueio de duplicidade: se o mesmo disparo for acionado mais de 1 vez em 1 minuto, bloqueia
            if (_ultimosDisparos.TryGetValue(chaveDisparo, out var ultimoEnvio) && (agoraUtc - ultimoEnvio) < JanelaBloqueioMesmoDisparo)
            {
                var decorrido = (agoraUtc - ultimoEnvio).TotalSeconds;
                _logger.LogWarning(
                    "[SMTP] Disparo bloqueado: o mesmo e-mail para {Destinatario} com assunto '{Assunto}' foi acionado mais de 1 vez em menos de 1 minuto ({Segundos:N1}s atrás).",
                    emailMascarado,
                    mensagem.Assunto,
                    decorrido);

                return false;
            }

            var stopwatch = Stopwatch.StartNew();

            for (var tentativa = 1; tentativa <= MaxTentativas; tentativa++)
            {
                try
                {
                    await EnviarViaSmtpAsync(mensagem, ct);
                    stopwatch.Stop();

                    _ultimosDisparos[chaveDisparo] = agoraUtc;

                    _logger.LogInformation(
                        "[SMTP] E-mail enviado com sucesso para {Destinatario} | Assunto: '{Assunto}' | Tentativa: {Tentativa}/{Max} | Duração: {ElapsedMilliseconds}ms",
                        emailMascarado,
                        mensagem.Assunto,
                        tentativa,
                        MaxTentativas,
                        stopwatch.ElapsedMilliseconds);

                    LimparChavesAntigas(agoraUtc);

                    return true;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    _logger.LogWarning("[SMTP] Envio de e-mail cancelado para {Destinatario}", emailMascarado);
                    return false;
                }
                catch (Exception ex) when (IsTransientException(ex) && tentativa < MaxTentativas)
                {
                    var delayMs = (int)Math.Pow(2, tentativa - 1) * 1000;
                    _logger.LogWarning(
                        ex,
                        "[SMTP] Falha transitória ao enviar e-mail para {Destinatario} (Tentativa {Tentativa}/{Max}). Retentando em {Delay}ms...",
                        emailMascarado,
                        tentativa,
                        MaxTentativas,
                        delayMs);

                    await Task.Delay(delayMs, ct);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    _logger.LogError(
                        ex,
                        "[SMTP] Falha definitiva no envio de e-mail para {Destinatario} | Assunto: '{Assunto}' | Tentativas: {Tentativas} | Duração: {ElapsedMilliseconds}ms",
                        emailMascarado,
                        mensagem.Assunto,
                        tentativa,
                        stopwatch.ElapsedMilliseconds);

                    return false;
                }
            }

            return false;
        }

        private async Task EnviarViaSmtpAsync(EmailMensagem mensagem, CancellationToken ct)
        {
            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
            mimeMessage.To.Add(new MailboxAddress(mensagem.NomeDestinatario ?? string.Empty, mensagem.Destinatario));
            mimeMessage.Subject = mensagem.Assunto;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = mensagem.CorpoHtml,
                TextBody = mensagem.CorpoTexto
            };
            mimeMessage.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            client.Timeout = _options.TimeoutSeconds * 1000;

            var secureSocketOptions = ObterSecureSocketOptions();

            await client.ConnectAsync(_options.Host, _options.Port, secureSocketOptions, ct);

            if (!string.IsNullOrWhiteSpace(_options.Username) && !string.IsNullOrWhiteSpace(_options.Password))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password, ct);
            }

            await client.SendAsync(mimeMessage, ct);

            try
            {
                await client.DisconnectAsync(true, ct);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[SMTP] Desconexão do servidor SMTP finalizada com aviso não fatal: {Message}", ex.Message);
            }
        }

        private SecureSocketOptions ObterSecureSocketOptions()
        {
            if (_options.UseSsl)
                return SecureSocketOptions.SslOnConnect;

            if (_options.Port == 587)
                return SecureSocketOptions.StartTls;

            if (_options.Port == 1025)
                return SecureSocketOptions.None;

            return SecureSocketOptions.Auto;
        }

        private static bool IsTransientException(Exception ex)
        {
            return ex is SmtpCommandException or SmtpProtocolException or IOException or SocketException or TimeoutException;
        }

        private static void LimparChavesAntigas(DateTime agoraUtc)
        {
            if (_ultimosDisparos.Count > 100)
            {
                var limite = agoraUtc.AddMinutes(-5);
                foreach (var item in _ultimosDisparos.Where(kvp => kvp.Value < limite).ToList())
                {
                    _ultimosDisparos.TryRemove(item.Key, out _);
                }
            }
        }

        public static void LimparHistoricoDisparos()
        {
            _ultimosDisparos.Clear();
        }

        public static void SimularDisparo(string destinatario, string assunto, DateTime quando)
        {
            var chave = $"{destinatario.Trim().ToLowerInvariant()}:{assunto.Trim()}";
            _ultimosDisparos[chave] = quando;
        }
    }
}
