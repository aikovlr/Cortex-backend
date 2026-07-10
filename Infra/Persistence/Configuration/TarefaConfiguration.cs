using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Cortex.Entities;

namespace Cortex.Infra.Persistence.Configuration
{
    public class TarefaConfiguration : IEntityTypeConfiguration<Tarefa>
    {
        public void Configure(EntityTypeBuilder<Tarefa> builder)
        {
            builder.ToTable("Tarefa");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Titulo)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(t => t.Descricao)
                .HasMaxLength(1000);

            builder.Property(t => t.DataVencimento)
                .IsRequired();

            builder.Property(t => t.Pontuacao)
                .IsRequired(false);
            
            builder.Property(t => t.Status)
                .HasColumnType("Status")
                .IsRequired();

            builder.Property(t => t.Prioridade)
                .HasColumnType("Prioridade")
                .IsRequired();

            builder.HasOne(t => t.Criador)
                .WithMany(u => u.TarefasCriadas)
                .HasForeignKey(t => t.CriadorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(t => t.ResponsaveisTarefas)
                .WithOne(rt => rt.Tarefa)
                .HasForeignKey(rt => rt.TarefaId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasMany(t => t.Respostas)
                .WithOne(r => r.Tarefa)
                .HasForeignKey(r => r.TarefaId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(t => t.Anexos)
                .WithOne(a => a.Tarefa)
                .HasForeignKey(a => a.TarefaId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(t => t.Tickets)
                .WithOne(tr => tr.Tarefa)
                .HasForeignKey(tr => tr.TarefaId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasMany(t => t.Sugestoes)
                .WithOne(s => s.Tarefa)
                .HasForeignKey(s => s.TarefaId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(t => t.Metas)
                .WithOne(m => m.Tarefa)
                .HasForeignKey(m => m.TarefaId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}