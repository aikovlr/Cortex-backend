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

            // Média de notas
            List<int> notas;
            if (filtro.CriadorId.HasValue)
            {
                notas = tarefas
                    .SelectMany(t => t.Respostas)
                    .Where(r => r.Nota.HasValue)
                    .Select(r => r.Nota!.Value)
                    .ToList();
            }
            else
            {
                notas = tarefas
                    .SelectMany(t => t.Respostas)
                    .Where(r => r.UsuarioId == solicitanteId && r.Nota.HasValue)
                    .Select(r => r.Nota!.Value)
                    .ToList();
            }

            double? mediaNotas = notas.Count > 0 ? Math.Round(notas.Average(), 1) : null;

            // Desempenho individual por responsável (visão de tarefas criadas ou quando houver responsáveis)
            var desempenhoResponsaveis = new List<DesempenhoResponsavelDTO>();
            var responsaveisDistintos = tarefas
                .SelectMany(t => t.ResponsaveisTarefas)
                .Where(rt => rt.Usuario != null)
                .Select(rt => rt.Usuario)
                .DistinctBy(u => u.Id)
                .ToList();

            foreach (var resp in responsaveisDistintos)
            {
                var tarefasDoResp = tarefas
                    .Where(t => t.ResponsaveisTarefas.Any(rt => rt.UsuarioId == resp.Id))
                    .ToList();

                var totalResp = tarefasDoResp.Count;
                var concluidasResp = tarefasDoResp.Count(t => t.Status == Status.Concluida);
                var atrasadasResp = tarefasDoResp.Count(t => t.Status == Status.Atrasada);
                var pendentesResp = tarefasDoResp.Count(t => t.Status == Status.Pendente);
                var taxaResp = totalResp > 0 ? Math.Round((double)concluidasResp / totalResp * 100, 2) : 0;

                var notasResp = tarefasDoResp
                    .SelectMany(t => t.Respostas)
                    .Where(r => r.UsuarioId == resp.Id && r.Nota.HasValue)
                    .Select(r => r.Nota!.Value)
                    .ToList();

                double? mediaNotasResp = notasResp.Count > 0 ? Math.Round(notasResp.Average(), 1) : null;

                desempenhoResponsaveis.Add(new DesempenhoResponsavelDTO
                {
                    UsuarioId = resp.Id,
                    Nome = resp.Nome,
                    Email = resp.Email,
                    TotalAtribuidas = totalResp,
                    Concluidas = concluidasResp,
                    Atrasadas = atrasadasResp,
                    Pendentes = pendentesResp,
                    TaxaConclusaoPercentual = taxaResp,
                    MediaNotas = mediaNotasResp
                });
            }

            var porPrioridade = Enum.GetValues<Prioridade>()
                .ToDictionary(p => p.ToString(), p => tarefas.Count(t => t.Prioridade == p));

            var porStatus = Enum.GetValues<Status>()
                .ToDictionary(s => s.ToString(), s => tarefas.Count(t => t.Status == s));

            _logger.LogInformation("Relatório de tarefas gerado para SolicitanteId {SolicitanteId}: Total {Total}, Concluídas {Concluidas}, Média Notas {MediaNotas}",
                solicitanteId, total, concluidas, mediaNotas);

            return new RelatorioTarefasDTO
            {
                TotalTarefas = total,
                Concluidas = concluidas,
                Atrasadas = atrasadas,
                Pendentes = pendentes,
                TaxaConclusaoPercentual = taxa,
                MediaNotas = mediaNotas,
                DesempenhoResponsaveis = desempenhoResponsaveis,
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

            var notasEquipe = tarefasEquipe
                .SelectMany(t => t.Respostas)
                .Where(r => r.Nota.HasValue)
                .Select(r => r.Nota!.Value)
                .ToList();
            double? mediaNotasGeral = notasEquipe.Count > 0 ? Math.Round(notasEquipe.Average(), 1) : null;

            var relatorioGeral = new RelatorioTarefasDTO
            {
                TotalTarefas = totalGeral,
                Concluidas = concluidasGeral,
                Atrasadas = atrasadasGeral,
                Pendentes = pendentesGeral,
                TaxaConclusaoPercentual = taxaGeral,
                MediaNotas = mediaNotasGeral,
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
            var responsaveisIds = filtro.ObterIdsResponsaveis();
            var membros = equipe.Membros.AsEnumerable();
            if (responsaveisIds.Count > 0)
            {
                membros = membros.Where(m => responsaveisIds.Contains(m.UsuarioId));
            }

            var desempenhoMembros = new List<DesempenhoMembroDTO>();
            foreach (var membro in membros)
            {
                var tarefasMembro = tarefasEquipe
                    .Where(t => t.ResponsaveisTarefas.Any(rt => rt.UsuarioId == membro.UsuarioId))
                    .ToList();

                var totalMembro = tarefasMembro.Count;
                var concluidasMembro = tarefasMembro.Count(t => t.Status == Status.Concluida);
                var atrasadasMembro = tarefasMembro.Count(t => t.Status == Status.Atrasada);
                var pendentesMembro = tarefasMembro.Count(t => t.Status == Status.Pendente);
                var taxaMembro = totalMembro > 0 ? Math.Round((double)concluidasMembro / totalMembro * 100, 2) : 0;

                var notasMembro = tarefasMembro
                    .SelectMany(t => t.Respostas)
                    .Where(r => r.UsuarioId == membro.UsuarioId && r.Nota.HasValue)
                    .Select(r => r.Nota!.Value)
                    .ToList();

                double? mediaNotasMembro = notasMembro.Count > 0 ? Math.Round(notasMembro.Average(), 1) : null;

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
                    TaxaConclusaoPercentual = taxaMembro,
                    MediaNotas = mediaNotasMembro
                });
            }

            _logger.LogInformation("Relatório consolidado gerado para a EquipeId {EquipeId} por SolicitanteId {SolicitanteId}",
                equipeId, solicitanteId);

            return new RelatorioEquipeDTO
            {
                EquipeId = equipe.Id,
                EquipeNome = equipe.Nome,
                TotalMembros = responsaveisIds.Count > 0 ? desempenhoMembros.Count : equipe.Membros.Count,
                MetricasGerais = relatorioGeral,
                DesempenhoMembros = desempenhoMembros
            };
        }
    }
}
