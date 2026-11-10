using System.IO;
using Cortex.DTOs;
using Cortex.Infra.Configuration;
using Cortex.Infra.Gateways;
using Cortex.Services.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace Cortex.Tests.Integration
{
    public class DispararNotificacoesEmailTeste
    {
        private readonly ITestOutputHelper _output;

        public DispararNotificacoesEmailTeste(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task EnviarTodasNotificacoesParaUsuario()
        {
            var basePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddUserSecrets("c5bfbcd1-827e-47f8-8df5-0b0006bc5501")
                .Build();

            var smtpOptions = new SmtpOptions();
            config.GetSection("Smtp").Bind(smtpOptions);

            _output.WriteLine($"Host: {smtpOptions.Host}, Port: {smtpOptions.Port}, User: {smtpOptions.Username}, From: {smtpOptions.FromAddress}, PassSet: {!string.IsNullOrEmpty(smtpOptions.Password)}");

            var emailDestino = config["TestSettings:EmailDestino"];
            var nomeDestino = config["TestSettings:NomeDestino"] ?? "Usuário Teste";

            if (string.IsNullOrWhiteSpace(emailDestino))
            {
                _output.WriteLine("Teste de disparo manual ignorado: defina 'TestSettings:EmailDestino' nos segredos/configuração para executar.");
                return;
            }

            var optionsWrapper = Options.Create(smtpOptions);
            var gateway = new SmtpEmailGateway(optionsWrapper, NullLogger<SmtpEmailGateway>.Instance);
            var templateService = new EmailTemplateService(config);

            var mensagens = new List<EmailMensagem>
            {
                templateService.GerarCodigoVerificacao(emailDestino, nomeDestino, "849201", 15),
                templateService.GerarCodigoRedefinicaoSenha(emailDestino, nomeDestino, "392014", 15),
                templateService.GerarConfirmacaoSenhaAlterada(emailDestino, nomeDestino),
                templateService.GerarNotificacaoEvento(emailDestino, nomeDestino, "tarefa", "Nova Tarefa Atribuída: Refatoração do Serviço de Notificações", "Você foi designado como responsável principal pela tarefa 'Refatoração do Serviço de Notificações' no projeto Cortex.", "/tarefas/104"),
                templateService.GerarNotificacaoEvento(emailDestino, nomeDestino, "tarefa_vencida", "Tarefa Vencida: Revisão de Arquitetura", "Atenção: A tarefa 'Revisão de Arquitetura' atingiu a data limite sem conclusão e foi marcada como atrasada.", "/tarefas/105"),
                templateService.GerarNotificacaoEvento(emailDestino, nomeDestino, "tarefa_urgente", "Prazo Próximo: Entrega da Versão 1.0", "Aviso de vencimento: Faltam menos de 24 horas para o prazo da tarefa 'Entrega da Versão 1.0'.", "/tarefas/106"),
                templateService.GerarNotificacaoEvento(emailDestino, nomeDestino, "meta", "Meta Concluída: MVP Cortex Finalizado", "Parabéns! Todas as tarefas vinculadas à meta 'MVP Cortex Finalizado' foram concluídas com sucesso.", "/metas/12"),
                templateService.GerarNotificacaoEvento(emailDestino, nomeDestino, "ticket", "Novo Ticket Aberto: Dúvida sobre integração SMTP", "Um novo ticket com prioridade alta foi atribuído a você: 'Dúvida sobre integração SMTP'.", "/tickets/88"),
                templateService.GerarNotificacaoEvento(emailDestino, nomeDestino, "sugestao", "Nova Sugestão Enviada: Dashboard em Tempo Real", "Uma nova sugestão da sua equipe foi submetida e aguarda avaliação: 'Dashboard em Tempo Real'.", "/sugestoes/23"),
                templateService.GerarNotificacaoEvento(emailDestino, nomeDestino, "equipe", "Boas-vindas à Equipe: Engenharia Cortex", "Você foi adicionado à equipe 'Engenharia Cortex'. Agora você pode colaborar nos projetos e tarefas deste grupo.", "/equipes/1"),
                templateService.GerarNotificacaoEvento(emailDestino, nomeDestino, "geral", "Atualização do Sistema: Notificações Ativadas", "O sistema de notificações por e-mail do Cortex está oficialmente configurado e ativo.", null)
            };

            var indice = 1;
            foreach (var msg in mensagens)
            {
                _output.WriteLine($"[{indice}/{mensagens.Count}] Enviando: {msg.Assunto} para {emailDestino}...");
                var enviado = await gateway.EnviarAsync(msg);
                Assert.True(enviado, $"Falha ao enviar e-mail: {msg.Assunto}");
                _output.WriteLine($"[{indice}/{mensagens.Count}] Enviado com sucesso!");
                indice++;
                await Task.Delay(1000);
            }
        }
    }
}

