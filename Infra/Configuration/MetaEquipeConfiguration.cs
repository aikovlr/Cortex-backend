using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Cortex.Entities;

namespace Cortex.Infra.Persistence.Configuration
{
    public class MetaEquipeConfiguration : IEntityTypeConfiguration<MetaEquipe>
    {
        public void Configure(EntityTypeBuilder<MetaEquipe> builder)
        {
            builder.ToTable("MetaEquipe");

            builder.HasKey(me => me.Id);

            builder.HasIndex(me => me.MetaId);
            builder.HasIndex(me => me.EquipeId);
            builder.HasIndex(me => new { me.MetaId, me.EquipeId });

            builder.Property(me => me.XpRecompensa)
                .IsRequired()
                .HasDefaultValue(0);

            builder.HasOne(me => me.Meta)
                .WithMany(m => m.MetasEquipes)
                .HasForeignKey(me => me.MetaId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(me => me.Equipe)
                .WithMany(e => e.MetasEquipes)
                .HasForeignKey(me => me.EquipeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

