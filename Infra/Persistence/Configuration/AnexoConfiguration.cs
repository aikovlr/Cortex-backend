using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Cortex.Entities;

namespace Cortex.Infra.Persistence.Configuration
{
    public class AnexoConfiguration : IEntityTypeConfiguration<Anexo>
    {
        public void Configure(EntityTypeBuilder<Anexo> builder)
        {
            builder.ToTable("Anexo");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.NomeOriginal)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(a => a.UrlCaminho)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(a => a.MimeType)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(a => a.DataEnvio)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(a => a.TipoEntidade)
                .HasColumnType("TipoEntidade")
                .IsRequired();

            builder.HasOne(a => a.Usuario)
                .WithOne(u => u.FotoPerfil)
                .HasForeignKey<Anexo>(a => a.UsuarioId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(a => a.Tarefa)
                .WithMany(t => t.Anexos)
                .HasForeignKey(a => a.TarefaId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(a => a.RespostaTarefa)
                .WithMany(rt => rt.Anexos)
                .HasForeignKey(a => a.RespostaTarefaId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}