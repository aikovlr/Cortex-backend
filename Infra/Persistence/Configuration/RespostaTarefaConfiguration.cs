using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Cortex.Entities;

namespace Cortex.Infra.Persistence.Configuration
{
    public class RespostaTarefaConfiguration : IEntityTypeConfiguration<RespostaTarefa>
    {
        public void Configure(EntityTypeBuilder<RespostaTarefa> builder)
        {
            builder.ToTable("RespostaTarefa");

            builder.HasKey(rt => rt.Id);

            builder.Property(rt => rt.DataResposta)
                .IsRequired();

            builder.HasOne(rt => rt.Usuario)
                .WithMany(u => u.Respostas)
                .HasForeignKey(rt => rt.UsuarioId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(rt => rt.Tarefa)
                .WithMany(t => t.Respostas)
                .HasForeignKey(rt => rt.TarefaId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(rt => rt.Anexos)
                .WithOne(a => a.RespostaTarefa)
                .HasForeignKey(a => a.RespostaTarefaId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}