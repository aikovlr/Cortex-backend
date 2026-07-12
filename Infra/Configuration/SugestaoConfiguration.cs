using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Cortex.Entities;

namespace Cortex.Infra.Persistence.Configuration
{
    public class SugestaoConfiguration : IEntityTypeConfiguration<Sugestao>
    {
        public void Configure(EntityTypeBuilder<Sugestao> builder)
        {
            builder.ToTable("Sugestao");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Descricao)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(s => s.DataCriacao)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.HasOne(s => s.Usuario)
                .WithMany(u => u.Sugestoes)
                .HasForeignKey(s => s.UsuarioId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(s => s.Tarefa)
                .WithMany(t => t.Sugestoes)
                .HasForeignKey(s => s.TarefaId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}