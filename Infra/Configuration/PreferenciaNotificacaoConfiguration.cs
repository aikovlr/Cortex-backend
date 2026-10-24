using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Cortex.Entities;

namespace Cortex.Infra.Persistence.Configuration
{
    public class PreferenciaNotificacaoConfiguration : IEntityTypeConfiguration<PreferenciaNotificacao>
    {
        public void Configure(EntityTypeBuilder<PreferenciaNotificacao> builder)
        {
            builder.ToTable("PreferenciasNotificacao");

            builder.HasKey(p => p.Id);

            builder.HasIndex(p => new { p.UsuarioId, p.Canal })
                .IsUnique();

            builder.Property(p => p.AntecedenciaHoras)
                .HasDefaultValue(24)
                .IsRequired();

            builder.Property(p => p.AtivoNovaAtribuicao)
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(p => p.AtivoVencimento)
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(p => p.AtivoConclusaoMeta)
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(p => p.AtivoTicket)
                .HasDefaultValue(true)
                .IsRequired();

            builder.HasOne(p => p.Usuario)
                .WithMany(u => u.PreferenciasNotificacao)
                .HasForeignKey(p => p.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
