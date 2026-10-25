using Cortex.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cortex.Infra.Persistence.Configuration
{
    public class AlertaEnviadoConfiguration : IEntityTypeConfiguration<AlertaEnviado>
    {
        public void Configure(EntityTypeBuilder<AlertaEnviado> builder)
        {
            builder.ToTable("AlertasEnviados");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.EnviadoEm)
                .IsRequired();

            builder.HasIndex(a => new { a.UsuarioId, a.TarefaId, a.Canal, a.EnviadoEm });

            builder.HasOne(a => a.Usuario)
                .WithMany()
                .HasForeignKey(a => a.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.Tarefa)
                .WithMany()
                .HasForeignKey(a => a.TarefaId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
