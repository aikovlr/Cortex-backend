using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Cortex.Entities;

namespace Cortex.Infra.Persistence.Configuration
{
    public class MetaTarefaConfiguration : IEntityTypeConfiguration<MetaTarefa>
    {
        public void Configure(EntityTypeBuilder<MetaTarefa> builder)
        {
            builder.ToTable("MetaTarefa");

            builder.HasKey(mt => mt.Id);

            builder.HasIndex(mt => mt.MetaId);
            builder.HasIndex(mt => mt.TarefaId);

            builder.HasOne(mt => mt.Meta)
                .WithMany(m => m.MetasTarefas)
                .HasForeignKey(mt => mt.MetaId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(mt => mt.Tarefa)
                .WithMany(t => t.MetasTarefas)
                .HasForeignKey(mt => mt.TarefaId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
