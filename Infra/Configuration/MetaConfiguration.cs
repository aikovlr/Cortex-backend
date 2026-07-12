using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Cortex.Entities;

namespace Cortex.Infra.Persistence.Configuration
{
    public class MetaConfiguration : IEntityTypeConfiguration<Meta>
    {
        public void Configure(EntityTypeBuilder<Meta> builder)
        {
            builder.ToTable("Meta");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Nome)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(m => m.DataFechamento)
                .IsRequired(false);

            builder.Property(m => m.Concluida)
                .IsRequired(false);

            builder.HasOne(m => m.Tarefa)
                .WithMany(t => t.Metas)
                .HasForeignKey(m => m.TarefaId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(m => m.Usuario)
                .WithMany(u => u.Metas)
                .HasForeignKey(m => m.UsuarioId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}