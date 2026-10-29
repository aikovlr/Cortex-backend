using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Cortex.Entities;

namespace Cortex.Infra.Persistence.Configuration
{
    public class NotificacaoConfiguration : IEntityTypeConfiguration<Notificacao>
    {
        public void Configure(EntityTypeBuilder<Notificacao> builder)
        {
            builder.ToTable("Notificacao");

            builder.HasKey(n => n.Id);

            builder.Property(n => n.Tipo)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(n => n.Titulo)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(n => n.Mensagem)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(n => n.Link)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(n => n.Lida)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(n => n.Destaque)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(n => n.DataCriacao)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.HasOne(n => n.Usuario)
                .WithMany(u => u.Notificacoes)
                .HasForeignKey(n => n.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(n => n.Tarefa)
                .WithMany(t => t.Notificacoes)
                .HasForeignKey(n => n.TarefaId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(n => new { n.UsuarioId, n.Lida });
            builder.HasIndex(n => new { n.UsuarioId, n.DataCriacao });
        }
    }
}

