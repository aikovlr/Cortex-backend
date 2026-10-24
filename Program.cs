using System.Text;
using Cortex.Entities;
using Cortex.Infra.Gateways;
using Cortex.Infra.Middleware;
using Cortex.Infra.Persistence;
using Cortex.Repositories.Implementations;
using Cortex.Repositories.Interface;
using Cortex.Services.Background;
using Cortex.Services.Implementations;
using Cortex.Services.Interfaces;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// Configuração do Serilog (RNF05)
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        new CompactJsonFormatter(),
        path: "logs/cortex-.json",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

builder.Host.UseSerilog();

// Adiciona suporte a Controllers
builder.Services.AddControllers();

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

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
            npgsqlOptions.MapEnum<CanalNotificacao>("CanalNotificacao");
        }));

// Injeção de Dependências - Repositórios
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<ITarefaRepository, TarefaRepository>();
builder.Services.AddScoped<IEquipeRepository, EquipeRepository>();
builder.Services.AddScoped<IMembroEquipeRepository, MembroEquipeRepository>();
builder.Services.AddScoped<IResponsavelTarefaRepository, ResponsavelTarefaRepository>();
builder.Services.AddScoped<IRespostaTarefaRepository, RespostaTarefaRepository>();
builder.Services.AddScoped<ISugestaoRepository, SugestaoRepository>();
builder.Services.AddScoped<ITicketReporteRepository, TicketReporteRepository>();
builder.Services.AddScoped<IMetaRepository, MetaRepository>();
builder.Services.AddScoped<IAnexoRepository, AnexoRepository>();

// Injeção de Dependências - Serviços
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IEquipeService, EquipeService>();
builder.Services.AddScoped<IMembroEquipeService, MembroEquipeService>();
builder.Services.AddScoped<ITarefaService, TarefaService>();
builder.Services.AddScoped<IResponsavelTarefaService, ResponsavelTarefaService>();
builder.Services.AddScoped<ISugestaoService, SugestaoService>();
builder.Services.AddScoped<ITicketReporteService, TicketReporteService>();
builder.Services.AddScoped<IMetaService, MetaService>();
builder.Services.AddScoped<IRespostaTarefaService, RespostaTarefaService>();
builder.Services.AddScoped<IAnexoService, AnexoService>();
builder.Services.AddScoped<IRelatorioService, RelatorioService>();

// Gateways de Notificação (Stubs)
builder.Services.AddScoped<IEmailGateway, EmailGatewayStub>();
builder.Services.AddScoped<IPushGateway, PushGatewayStub>();
builder.Services.AddScoped<ISmsGateway, SmsGatewayStub>();
builder.Services.AddScoped<INotificacaoService, NotificacaoService>();

// Background Services (RN06, RNF10)
builder.Services.AddHostedService<TarefaStatusBackgroundService>();
builder.Services.AddHostedService<AlertaVencimentoBackgroundService>();

// Autenticação JWT & Autorização
var jwtSecret = builder.Configuration["Jwt:Secret"] 
    ?? throw new InvalidOperationException("Configuração 'Jwt:Secret' não encontrada.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CortexApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CortexClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Middleware Global de Exceções
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// Mapeia os Controllers
app.MapControllers();

app.Run();