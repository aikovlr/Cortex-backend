using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Cortex.Entities;

namespace Cortex.Infra.Persistence.Configuration
{
    public class MembroEquipeConfiguration : IEntityTypeConfiguration<MembroEquipe>
    {
        public void Configure(EntityTypeBuilder<MembroEquipe> builder)
        {
            builder.ToTable("MembroEquipe");

            builder.HasKey(me => me.Id);

            builder.Property(me => me.DataEntrada)
                .IsRequired();

            builder.Property(me => me.Cargo)
                .HasColumnType("Cargo")
                .IsRequired();

            builder.HasOne(me => me.Usuario)
                .WithMany(u => u.Equipes)
                .HasForeignKey(me => me.UsuarioId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(me => me.Equipe)
                .WithMany(e => e.Membros)
                .HasForeignKey(me => me.EquipeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}