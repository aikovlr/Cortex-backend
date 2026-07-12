using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Cortex.Entities;

namespace Cortex.Infra.Persistence.Configuration
{
    public class ResponsavelTarefaConfiguration : IEntityTypeConfiguration<ResponsavelTarefa>
    {
        public void Configure(EntityTypeBuilder<ResponsavelTarefa> builder)
        {
            builder.ToTable("ResponsavelTarefa");

            builder.HasKey(rt => rt.Id);

            builder.HasOne(rt => rt.Usuario)
                .WithMany(u => u.ResponsaveisTarefas)
                .HasForeignKey(rt => rt.UsuarioId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(rt => rt.Tarefa)
                .WithMany(t => t.ResponsaveisTarefas)
                .HasForeignKey(rt => rt.TarefaId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}