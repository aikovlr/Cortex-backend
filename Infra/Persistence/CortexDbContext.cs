using Cortex.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cortex.Infra.Persistence;

public class CortexDbContext : DbContext
{
    public CortexDbContext(DbContextOptions<CortexDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Tarefa> Tarefas => Set<Tarefa>();
    public DbSet<Equipe> Equipes => Set<Equipe>();
    public DbSet<MembroEquipe> MembrosEquipe => Set<MembroEquipe>();
    public DbSet<RespostaTarefa> RespostasTarefa => Set<RespostaTarefa>();
    public DbSet<ResponsavelTarefa> ResponsaveisTarefa => Set<ResponsavelTarefa>();
    public DbSet<Anexo> Anexos => Set<Anexo>();
    public DbSet<TicketReporte> TicketReporte => Set<TicketReporte>();
    public DbSet<Sugestao> Sugestoes => Set<Sugestao>();
    public DbSet<Meta> Metas => Set<Meta>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresEnum<Status>("Status");
        modelBuilder.HasPostgresEnum<Prioridade>("Prioridade");
        modelBuilder.HasPostgresEnum<TipoEntidade>("TipoEntidade");
        modelBuilder.HasPostgresEnum<Cargo>("Cargo");
        
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CortexDbContext).Assembly);
    }
}