using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Cortex.Entities;

namespace Cortex.Infra.Persistence.Configuration
{
    public class TicketReporteConfiguration : IEntityTypeConfiguration<TicketReporte>
    {
        public void Configure(EntityTypeBuilder<TicketReporte> builder)
        {
            builder.ToTable("TicketReporte");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Descricao)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(t => t.Ativo)
                .IsRequired();

            builder.Property(t => t.DataCriacao)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.HasOne(t => t.Usuario)
                .WithMany(u => u.Tickets)
                .HasForeignKey(t => t.UsuarioId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(t => t.Tarefa)
                .WithMany(t => t.Tickets)
                .HasForeignKey(t => t.TarefaId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}