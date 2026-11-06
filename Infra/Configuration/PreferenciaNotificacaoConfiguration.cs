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

            builder.Property(p => p.AtivoSugestao)
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(p => p.AtivoEquipe)
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(p => p.AtivoGeral)
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(p => p.Tema)
                .HasMaxLength(20)
                .HasDefaultValue("light")
                .IsRequired();

            builder.Property(p => p.AltoContraste)
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(p => p.TamanhoFonte)
                .HasDefaultValue(16)
                .IsRequired();

            builder.HasOne(p => p.Usuario)
                .WithMany(u => u.PreferenciasNotificacao)
                .HasForeignKey(p => p.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
