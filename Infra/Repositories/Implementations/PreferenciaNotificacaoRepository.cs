using Cortex.Entities;
using Cortex.Infra.Persistence;
using Cortex.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace Cortex.Repositories.Implementations
{
    public class PreferenciaNotificacaoRepository : IPreferenciaNotificacaoRepository
    {
        private readonly CortexDbContext _context;

        public PreferenciaNotificacaoRepository(CortexDbContext context)
        {
            _context = context;
        }

        public async Task<PreferenciaNotificacao?> ObterPorUsuarioECanalAsync(int usuarioId, CanalNotificacao canal)
            => await _context.PreferenciasNotificacao
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId && p.Canal == canal);

        public async Task<List<PreferenciaNotificacao>> ObterPorUsuarioAsync(int usuarioId)
            => await _context.PreferenciasNotificacao
                .Where(p => p.UsuarioId == usuarioId)
                .ToListAsync();

        public async Task<PreferenciaNotificacao> AdicionarAsync(PreferenciaNotificacao preferencia)
        {
            _context.PreferenciasNotificacao.Add(preferencia);
            await _context.SaveChangesAsync();
            return preferencia;
        }

        public async Task<PreferenciaNotificacao> AtualizarAsync(PreferenciaNotificacao preferencia)
        {
            _context.PreferenciasNotificacao.Update(preferencia);
            await _context.SaveChangesAsync();
            return preferencia;
        }

        public async Task AtualizarAcessibilidadeUsuarioAsync(int usuarioId, string? tema, bool? altoContraste, int? tamanhoFonte)
        {
            var preferencias = await _context.PreferenciasNotificacao
                .Where(p => p.UsuarioId == usuarioId)
                .ToListAsync();

            if (preferencias.Count == 0)
            {
                var canais = Enum.GetValues<CanalNotificacao>();
                foreach (var canal in canais)
                {
                    _context.PreferenciasNotificacao.Add(new PreferenciaNotificacao
                    {
                        UsuarioId = usuarioId,
                        Canal = canal,
                        AntecedenciaHoras = 24,
                        AtivoNovaAtribuicao = true,
                        AtivoVencimento = true,
                        AtivoConclusaoMeta = true,
                        AtivoTicket = true,
                        Tema = tema ?? "light",
                        AltoContraste = altoContraste ?? false,
                        TamanhoFonte = tamanhoFonte ?? 16
                    });
                }
            }
            else
            {
                foreach (var pref in preferencias)
                {
                    if (tema != null) pref.Tema = tema;
                    if (altoContraste.HasValue) pref.AltoContraste = altoContraste.Value;
                    if (tamanhoFonte.HasValue) pref.TamanhoFonte = tamanhoFonte.Value;
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
