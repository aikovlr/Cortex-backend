using Microsoft.EntityFrameworkCore;
using Cortex.DTOs.Request;
using Cortex.DTOs.Response;
using Cortex.Entities;
using Cortex.Infra.Exceptions;
using Cortex.Infra.Persistence;
using Cortex.Repositories.Interface;
using Cortex.Services.Interfaces;

namespace Cortex.Services.Implementations
{
    public class RelatorioService : IRelatorioService
    {
        private readonly CortexDbContext _context;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IEquipeRepository _equipeRepository;
        private readonly ILogger<RelatorioService> _logger;

        public RelatorioService(
            CortexDbContext context,
            IUsuarioRepository usuarioRepository,
            IEquipeRepository equipeRepository,
            ILogger<RelatorioService> logger)
        {
            _context = context;
            _usuarioRepository = usuarioRepository;
            _equipeRepository = equipeRepository;
            _logger = logger;
        }

        public async Task<RelatorioTarefasDTO> GerarRelatorioTarefasAsync(FiltroRelatorioDTO filtro, int solicitanteId)
        {
            var usuario = await _usuarioRepository.ObterUsuarioPorIdAsync(solicitanteId);
            if (usuario == null)
                throw new KeyNotFoundException("Usuário solicitante não encontrado.");

            var ehAdminOuDono = usuario.Equipes.Any(e => e.Cargo == Cargo.Dono || e.Cargo == Cargo.Admin);

            var query = _context.Tarefas
                .AsNoTracking()
                .Include(t => t.ResponsaveisTarefas)
                .AsQueryable();

            // RN09: Usuários comuns (Membro) só podem gerar relatórios de suas próprias tarefas
            if (!ehAdminOuDono)
            {
                query = query.Where(t => t.CriadorId == solicitanteId || t.ResponsaveisTarefas.Any(rt => rt.UsuarioId == solicitanteId));
            }
            else
            {
                // RN10: Dono e Admin podem aplicar filtros gerais livremente
                if (filtro.ResponsavelId.HasValue)
                {
                    query = query.Where(t => t.ResponsaveisTarefas.Any(rt => rt.UsuarioId == filtro.ResponsavelId.Value));
                }

                if (filtro.EquipeId.HasValue)
                {
                    var membrosEquipeIds = await _context.MembrosEquipe
                        .Where(m => m.EquipeId == filtro.EquipeId.Value)
                        .Select(m => m.UsuarioId)
                        .ToListAsync();

                    query = query.Where(t => t.ResponsaveisTarefas.Any(rt => membrosEquipeIds.Contains(rt.UsuarioId)));
                }
            }

            // Filtros comuns
            if (filtro.Status.HasValue)
            {
                query = query.Where(t => t.Status == filtro.Status.Value);
            }

            if (filtro.Prioridade.HasValue)
            {
                query = query.Where(t => t.Prioridade == filtro.Prioridade.Value);
            }

            if (filtro.DataInicio.HasValue)
            {
                var inicioUtc = filtro.DataInicio.Value.Kind == DateTimeKind.Utc 
                    ? filtro.DataInicio.Value 
                    : DateTime.SpecifyKind(filtro.DataInicio.Value, DateTimeKind.Utc);
                query = query.Where(t => t.DataVencimento >= inicioUtc);
            }

            if (filtro.DataFim.HasValue)
            {
                var fimUtc = filtro.DataFim.Value.Kind == DateTimeKind.Utc 
                    ? filtro.DataFim.Value 
                    : DateTime.SpecifyKind(filtro.DataFim.Value, DateTimeKind.Utc);
                query = query.Where(t => t.DataVencimento <= fimUtc);
            }

            var tarefas = await query.ToListAsync();

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
                    CriadorId = t.CriadorId
                }).ToList()
            };
        }

        public async Task<RelatorioEquipeDTO> GerarRelatorioEquipeAsync(int equipeId, FiltroRelatorioDTO filtro, int solicitanteId)
        {
            var equipe = await _context.Equipes
                .Include(e => e.Membros)
                    .ThenInclude(m => m.Usuario)
                .FirstOrDefaultAsync(e => e.Id == equipeId);

            if (equipe == null)
                throw new KeyNotFoundException("Equipe não encontrada.");

            // RN10: Apenas Dono ou Admin da equipe podem extrair relatório consolidado
            var membroSolicitante = equipe.Membros.FirstOrDefault(m => m.UsuarioId == solicitanteId);
            if (membroSolicitante == null || (membroSolicitante.Cargo != Cargo.Dono && membroSolicitante.Cargo != Cargo.Admin))
            {
                throw new ForbiddenException("Apenas o Dono ou Admin da equipe podem gerar relatórios consolidados da equipe (RN10).");
            }

            var membroIds = equipe.Membros.Select(m => m.UsuarioId).ToList();

            // Busca tarefas atribuídas aos membros da equipe
            var query = _context.Tarefas
                .AsNoTracking()
                .Include(t => t.ResponsaveisTarefas)
                .Where(t => t.ResponsaveisTarefas.Any(rt => membroIds.Contains(rt.UsuarioId)));

            if (filtro.Status.HasValue)
                query = query.Where(t => t.Status == filtro.Status.Value);

            if (filtro.Prioridade.HasValue)
                query = query.Where(t => t.Prioridade == filtro.Prioridade.Value);

            if (filtro.DataInicio.HasValue)
            {
                var inicioUtc = filtro.DataInicio.Value.Kind == DateTimeKind.Utc 
                    ? filtro.DataInicio.Value 
                    : DateTime.SpecifyKind(filtro.DataInicio.Value, DateTimeKind.Utc);
                query = query.Where(t => t.DataVencimento >= inicioUtc);
            }

            if (filtro.DataFim.HasValue)
            {
                var fimUtc = filtro.DataFim.Value.Kind == DateTimeKind.Utc 
                    ? filtro.DataFim.Value 
                    : DateTime.SpecifyKind(filtro.DataFim.Value, DateTimeKind.Utc);
                query = query.Where(t => t.DataVencimento <= fimUtc);
            }

            var tarefasEquipe = await query.ToListAsync();

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
                    CriadorId = t.CriadorId
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
