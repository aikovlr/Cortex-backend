using Cortex.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Cortex.Entities;

var builder = WebApplication.CreateBuilder(args);

// Adiciona suporte a Controllers
builder.Services.AddControllers();

// OpenAPI
builder.Services.AddOpenApi();

// Entity Framework
builder.Services.AddDbContext<CortexDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.MapEnum<Status>("Status");
            npgsqlOptions.MapEnum<Prioridade>("Prioridade");
            npgsqlOptions.MapEnum<TipoEntidade>("TipoEntidade");
            npgsqlOptions.MapEnum<Cargo>("Cargo");
        }));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Mapeia os Controllers
app.MapControllers();

app.Run();