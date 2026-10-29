using Cortex.DTOs.Request;
using Cortex.DTOs.Response;
using Cortex.Entities;
using Cortex.Infra.Exceptions;
using Cortex.Repositories.Interface;
using Cortex.Services.Interfaces;

namespace Cortex.Services.Implementations
{
    public class RelatorioService : IRelatorioService
    {
        private readonly IRelatorioRepository _relatorioRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IEquipeRepository _equipeRepository;
        private readonly IEquipeAuthorizationService _authService;
        private readonly ILogger<RelatorioService> _logger;

        public RelatorioService(
            IRelatorioRepository relatorioRepository,
            IUsuarioRepository usuarioRepository,
            IEquipeRepository equipeRepository,
            IEquipeAuthorizationService authService,
            ILogger<RelatorioService> logger)
        {
            _relatorioRepository = relatorioRepository;
            _usuarioRepository = usuarioRepository;
            _equipeRepository = equipeRepository;
            _authService = authService;
            _logger = logger;
        }

        public async Task<RelatorioTarefasDTO> GerarRelatorioTarefasAsync(FiltroRelatorioDTO filtro, int solicitanteId)
        {
            var usuario = await _usuarioRepository.ObterUsuarioPorIdAsync(solicitanteId);
            if (usuario == null)
                throw new KeyNotFoundException("Usuário solicitante não encontrado.");

            bool ehAdminOuDono;
            if (filtro.EquipeId.HasValue)
            {
                ehAdminOuDono = await _authService.EhAdminOuDonoDaEquipeAsync(solicitanteId, filtro.EquipeId.Value);
            }
            else
            {
                ehAdminOuDono = usuario.Equipes.Any(e => e.Cargo == Cargo.Dono || e.Cargo == Cargo.Admin);
            }

            var tarefas = await _relatorioRepository.ObterTarefasParaRelatorioAsync(solicitanteId, ehAdminOuDono, filtro);

            var total = tarefas.Count;
            var concluidas = tarefas.Count(t => t.Status == Status.Concluida);
            var atrasadas = tarefas.Count(t => t.Status == Status.Atrasada);
            var pendentes = tarefas.Count(t => t.Status == Status.Pendente);
            var taxa = total > 0 ? Math.Round((double)concluidas / total * 100, 2) : 0;

            var porPrioridade = Enum.GetValues<Prioridade>()
                .ToDictionary(p => p.ToString(), p => tarefas.Count(t => t.Prioridade == p));

            var porStatus = Enum.GetValues<Status>()
                .ToDictionary(s => s.ToString(), s => tarefas.Count(t => t.Status == s));

            _logger.LogInformation("Relatório de tarefas gerado para SolicitanteId {SolicitanteId}: Total {Total}, Concluídas {Concluidas}",
                solicitanteId, total, concluidas);

            return new RelatorioTarefasDTO
            {
                TotalTarefas = total,
                Concluidas = concluidas,
                Atrasadas = atrasadas,
                Pendentes = pendentes,
                TaxaConclusaoPercentual = taxa,
                PorPrioridade = porPrioridade,
                PorStatus = porStatus,
                PeriodoInicio = filtro.DataInicio,
                PeriodoFim = filtro.DataFim,
                GeradoEm = DateTime.UtcNow,
                Tarefas = tarefas.Select(t => new ConsultaTarefaDTO
                {
                    Id = t.Id,
                    Titulo = t.Titulo,
                    Descricao = t.Descricao,
                    DataVencimento = t.DataVencimento,
                    Pontuacao = t.Pontuacao,
                    Status = t.Status,
                    Prioridade = t.Prioridade,
                    CriadorId = t.CriadorId,
                    DataCriacao = t.DataCriacao
                }).ToList()
            };
        }

        public async Task<RelatorioEquipeDTO> GerarRelatorioEquipeAsync(int equipeId, FiltroRelatorioDTO filtro, int solicitanteId)
        {
            var equipe = await _equipeRepository.ObterEquipePorIdAsync(equipeId);

            if (equipe == null)
                throw new KeyNotFoundException("Equipe não encontrada.");

            // RN10: Apenas Dono ou Admin da equipe podem extrair relatório consolidado
            await _authService.ExigirAdminOuDonoDaEquipeAsync(solicitanteId, equipeId);

            var tarefasEquipe = await _relatorioRepository.ObterTarefasDaEquipeAsync(equipeId, filtro);

            var totalGeral = tarefasEquipe.Count;
            var concluidasGeral = tarefasEquipe.Count(t => t.Status == Status.Concluida);
            var atrasadasGeral = tarefasEquipe.Count(t => t.Status == Status.Atrasada);
            var pendentesGeral = tarefasEquipe.Count(t => t.Status == Status.Pendente);
            var taxaGeral = totalGeral > 0 ? Math.Round((double)concluidasGeral / totalGeral * 100, 2) : 0;

            var relatorioGeral = new RelatorioTarefasDTO
            {
                TotalTarefas = totalGeral,
                Concluidas = concluidasGeral,
                Atrasadas = atrasadasGeral,
                Pendentes = pendentesGeral,
                TaxaConclusaoPercentual = taxaGeral,
                PorPrioridade = Enum.GetValues<Prioridade>()
                    .ToDictionary(p => p.ToString(), p => tarefasEquipe.Count(t => t.Prioridade == p)),
                PorStatus = Enum.GetValues<Status>()
                    .ToDictionary(s => s.ToString(), s => tarefasEquipe.Count(t => t.Status == s)),
                PeriodoInicio = filtro.DataInicio,
                PeriodoFim = filtro.DataFim,
                GeradoEm = DateTime.UtcNow,
                Tarefas = tarefasEquipe.Select(t => new ConsultaTarefaDTO
                {
                    Id = t.Id,
                    Titulo = t.Titulo,
                    Descricao = t.Descricao,
                    DataVencimento = t.DataVencimento,
                    Pontuacao = t.Pontuacao,
                    Status = t.Status,
                    Prioridade = t.Prioridade,
                    CriadorId = t.CriadorId,
                    DataCriacao = t.DataCriacao
                }).ToList()
            };

            // Desempenho individual por membro
            var desempenhoMembros = new List<DesempenhoMembroDTO>();
            foreach (var membro in equipe.Membros)
            {
                var tarefasMembro = tarefasEquipe
                    .Where(t => t.ResponsaveisTarefas.Any(rt => rt.UsuarioId == membro.UsuarioId))
                    .ToList();

                var totalMembro = tarefasMembro.Count;
                var concluidasMembro = tarefasMembro.Count(t => t.Status == Status.Concluida);
                var atrasadasMembro = tarefasMembro.Count(t => t.Status == Status.Atrasada);
                var pendentesMembro = tarefasMembro.Count(t => t.Status == Status.Pendente);
                var taxaMembro = totalMembro > 0 ? Math.Round((double)concluidasMembro / totalMembro * 100, 2) : 0;

                desempenhoMembros.Add(new DesempenhoMembroDTO
                {
                    UsuarioId = membro.UsuarioId,
                    Nome = membro.Usuario?.Nome ?? "Usuário",
                    Email = membro.Usuario?.Email ?? "",
                    Cargo = membro.Cargo,
                    TotalAtribuidas = totalMembro,
                    Concluidas = concluidasMembro,
                    Atrasadas = atrasadasMembro,
                    Pendentes = pendentesMembro,
                    TaxaConclusaoPercentual = taxaMembro
                });
            }

            _logger.LogInformation("Relatório consolidado gerado para a EquipeId {EquipeId} por SolicitanteId {SolicitanteId}",
                equipeId, solicitanteId);

            return new RelatorioEquipeDTO
            {
                EquipeId = equipe.Id,
                EquipeNome = equipe.Nome,
                TotalMembros = equipe.Membros.Count,
                MetricasGerais = relatorioGeral,
                DesempenhoMembros = desempenhoMembros
            };
        }
    }
}
