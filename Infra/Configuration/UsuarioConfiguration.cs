using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Cortex.Entities;

namespace Cortex.Infra.Persistence.Configuration
{
    public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable("Usuario");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Nome)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(u => u.Telefone)
                .HasMaxLength(20);

            builder.HasIndex(u => u.Email)
                .IsUnique();

            builder.Property(u => u.SenhaHash)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(u => u.DataCriacao)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.HasOne(u => u.FotoPerfil)
                .WithOne()
                .HasForeignKey<Anexo>(u => u.Id)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(u => u.TarefasCriadas)
                .WithOne(t => t.Criador)
                .HasForeignKey(t => t.CriadorId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasMany(u => u.Respostas)
                .WithOne(r => r.Usuario)
                .HasForeignKey(r => r.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.ResponsaveisTarefas)
                .WithOne(rt => rt.Usuario)
                .HasForeignKey(rt => rt.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasMany(u => u.Equipes)
                .WithOne(me => me.Usuario)
                .HasForeignKey(me => me.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.Tickets)
                .WithOne(t => t.Usuario)
                .HasForeignKey(t => t.UsuarioId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasMany(u => u.Sugestoes)
                .WithOne(s => s.Usuario)
                .HasForeignKey(s => s.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasMany(u => u.Metas)
                .WithOne(m => m.Usuario)
                .HasForeignKey(m => m.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}