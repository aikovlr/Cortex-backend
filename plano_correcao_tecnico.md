# 🔧 Plano de Correção Técnico — Cortex Backend

> **Referência:** [Análise de Falhas](file:///C:/Users/yuma2/.gemini/antigravity-ide/brain/6b4f284c-3334-45e4-98a3-c89676874336/analise_cortex_backend.md)  
> **Stack:** C# · .NET 10 · ASP.NET Core · EF Core · PostgreSQL  
> **Princípios:** SOLID · Clean Architecture · Repository Pattern

---

## 🔴 BLOCO 1 — IMEDIATO (antes de qualquer deploy)

---

### [SEC-01] Credenciais — Preparar para Produção

**Princípio SOLID:** *Open/Closed* — a configuração deve ser aberta para extensão por ambiente sem alterar o código.

**Passo 1 — Adicionar ao `.gitignore`:**
```
# Configurações de ambiente com segredos
appsettings.Production.json
appsettings.Staging.json
```

**Passo 2 — Usar User Secrets em desenvolvimento (já existe suporte nativo no .NET):**
```bash
# No terminal, na raiz do projeto
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=Cortex;Username=postgres;Password=SUA_SENHA_LOCAL"
dotnet user-secrets set "Jwt:Secret" "SEU_SECRET_LOCAL_MIN_32_CHARS"
```

**Passo 3 — Limpar `appsettings.Development.json` (remover connection string):**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

**Passo 4 — `appsettings.json` deve conter apenas estrutura sem valores sensíveis:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Jwt": {
    "Secret": "",
    "Issuer": "CortexApi",
    "Audience": "CortexClient",
    "ExpiresInMinutes": 60
  }
}
```

**Passo 5 — Validar obrigatoriedade no `Program.cs` (já existe, manter):**
```csharp
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Configuração 'Jwt:Secret' não encontrada.");
```

---

### [SEC-02 + SEC-03] Verificação de E-mail — Código Seguro e Persistido

**Princípios SOLID aplicados:**
- *Single Responsibility*: `PasswordService` cuida de senhas; o novo `IVerificacaoEmailService` cuida de códigos de verificação.
- *Open/Closed*: A geração de código é abstraída por interface, permitindo trocar a estratégia futuramente.
- *Dependency Inversion*: Services dependem de abstrações, não de implementações concretas.

**Passo 1 — Adicionar campos à entidade `Usuario`** em [`Entities/Usuario.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Entities/Usuario.cs):
```csharp
public class Usuario
{
    // ... campos existentes ...
    public bool EmailVerificado { get; set; } = false;

    // Novos campos para verificação segura
    public string? CodigoVerificacaoHash { get; set; }
    public DateTime? CodigoVerificacaoExpiracao { get; set; }
}
```

**Passo 2 — Criar interface `IVerificacaoEmailService`** em `Services/Interfaces/IVerificacaoEmailService.cs`:
```csharp
namespace Cortex.Services.Interfaces
{
    public interface IVerificacaoEmailService
    {
        /// <summary>Gera um código de 6 dígitos, armazena o hash no usuário e retorna o código em texto claro para envio.</summary>
        Task<string> GerarEArmazenarCodigoAsync(int usuarioId);

        /// <summary>Verifica o código recebido contra o hash armazenado e marca o e-mail como verificado.</summary>
        Task<bool> VerificarCodigoAsync(string email, string codigoRecebido);
    }
}
```

**Passo 3 — Implementar `VerificacaoEmailService`** em `Services/Implementations/VerificacaoEmailService.cs`:
```csharp
using System.Security.Cryptography;
using Cortex.Infra.Exceptions;
using Cortex.Repositories.Interface;
using Cortex.Services.Interfaces;

namespace Cortex.Services.Implementations
{
    public class VerificacaoEmailService : IVerificacaoEmailService
    {
        private const int CodigoExpiracaoMinutos = 15;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly ILogger<VerificacaoEmailService> _logger;

        public VerificacaoEmailService(
            IUsuarioRepository usuarioRepository,
            ILogger<VerificacaoEmailService> logger)
        {
            _usuarioRepository = usuarioRepository;
            _logger = logger;
        }

        public async Task<string> GerarEArmazenarCodigoAsync(int usuarioId)
        {
            var usuario = await _usuarioRepository.ObterUsuarioPorIdAsync(usuarioId)
                ?? throw new KeyNotFoundException("Usuário não encontrado.");

            // Gera código criptograficamente seguro de 6 dígitos
            var codigo = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            // Armazena o hash do código (nunca o texto claro)
            usuario.CodigoVerificacaoHash = BCrypt.Net.BCrypt.HashPassword(codigo);
            usuario.CodigoVerificacaoExpiracao = DateTime.UtcNow.AddMinutes(CodigoExpiracaoMinutos);

            await _usuarioRepository.AtualizarUsuarioAsync(usuario);

            // NUNCA logar o código em texto claro
            _logger.LogInformation(
                "Código de verificação gerado para UsuarioId {UsuarioId}. Expira em {Expiracao} UTC.",
                usuarioId, usuario.CodigoVerificacaoExpiracao);

            return codigo; // Retornado apenas para envio via gateway de e-mail
        }

        public async Task<bool> VerificarCodigoAsync(string email, string codigoRecebido)
        {
            var usuario = await _usuarioRepository.ObterUsuarioPorEmailAsync(email)
                ?? throw new KeyNotFoundException("Usuário não encontrado.");

            if (string.IsNullOrWhiteSpace(usuario.CodigoVerificacaoHash) ||
                !usuario.CodigoVerificacaoExpiracao.HasValue)
                throw new InvalidOperationException("Nenhum código de verificação foi solicitado para este e-mail.");

            if (DateTime.UtcNow > usuario.CodigoVerificacaoExpiracao.Value)
                throw new InvalidOperationException("O código de verificação expirou. Solicite um novo código.");

            var codigoValido = BCrypt.Net.BCrypt.Verify(codigoRecebido, usuario.CodigoVerificacaoHash);
            if (!codigoValido)
                throw new ArgumentException("Código de verificação inválido.");

            // Invalida o código após uso (uso único)
            usuario.EmailVerificado = true;
            usuario.CodigoVerificacaoHash = null;
            usuario.CodigoVerificacaoExpiracao = null;

            await _usuarioRepository.AtualizarUsuarioAsync(usuario);

            _logger.LogInformation(
                "E-mail verificado com sucesso para UsuarioId {UsuarioId}.", usuario.Id);

            return true;
        }
    }
}
```

**Passo 4 — Atualizar `UsuarioService`** — remover a lógica de verificação inline e delegar ao novo service:
```csharp
// Remover os métodos VerificarEmailAsync e SolicitarCodigoVerificacaoAsync do UsuarioService
// A lógica agora pertence ao IVerificacaoEmailService (SRP)
```

**Passo 5 — Atualizar `AuthController`** para injetar e usar o novo service:
```csharp
// Injetar IVerificacaoEmailService no construtor
private readonly IVerificacaoEmailService _verificacaoEmailService;

[HttpPost("solicitar-verificacao-email")]
[AllowAnonymous]
[EnableRateLimiting("verificacao-email-policy")] // rate limiting (ver SEC-05)
public async Task<IActionResult> SolicitarCodigoVerificacao([FromBody] SolicitarCodigoVerificacaoDTO dto)
{
    var usuario = await _usuarioRepository.ObterUsuarioPorEmailAsync(dto.Email);
    if (usuario == null)
        return Ok(new { message = "Se o e-mail existir, um código foi enviado." }); // não revelar existência

    var codigo = await _verificacaoEmailService.GerarEArmazenarCodigoAsync(usuario.Id);
    await _emailGateway.EnviarEmailAsync(dto.Email, "Verificação de e-mail", $"Seu código: {codigo}");

    return Ok(new { message = "Se o e-mail existir, um código foi enviado." });
}

[HttpPost("verificar-email")]
[AllowAnonymous]
public async Task<IActionResult> VerificarEmail([FromBody] VerificarEmailDTO dto)
{
    await _verificacaoEmailService.VerificarCodigoAsync(dto.Email, dto.Codigo);
    return Ok(new { message = "E-mail verificado com sucesso." }); // não retornar email na resposta
}
```

**Passo 6 — Registrar no `Program.cs`:**
```csharp
builder.Services.AddScoped<IVerificacaoEmailService, VerificacaoEmailService>();
```

**Passo 7 — Gerar migration:**
```bash
dotnet ef migrations add AddVerificacaoEmailFields
dotnet ef database update
```

---

### [SEC-04] `RequireHttpsMetadata` Condicional

**Arquivo:** [`Program.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Program.cs#L110)

**Princípio SOLID:** *Open/Closed* — comportamento diferente por ambiente sem alterar a lógica central.

```csharp
.AddJwtBearer(options =>
{
    // Em produção, SEMPRE exigir HTTPS para validação de metadados
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    // ... demais configurações permanecem iguais
});
```

---

### [SEC-05] Ordem do Pipeline de Middleware

**Arquivo:** [`Program.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Program.cs#L140-L158)

**Princípio:** O pipeline de middleware segue a ordem de registro — rate limiting deve ser aplicado antes de qualquer processamento de autenticação.

```csharp
// ORDEM CORRETA do pipeline:
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRateLimiter();        // ← 1º: bloqueia requisições excessivas antes de tudo
app.UseAuthentication();     // ← 2º: valida o token
app.UseAuthorization();      // ← 3º: verifica permissões

app.MapControllers();
app.Run();
```

Também adicionar policy de rate limiting para o endpoint de verificação de e-mail:
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("login-policy", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });

    // Novo: rate limiting para verificação de e-mail
    options.AddFixedWindowLimiter("verificacao-email-policy", opt =>
    {
        opt.PermitLimit = 3;
        opt.Window = TimeSpan.FromMinutes(5);
        opt.QueueLimit = 0;
    });
});
```

---

### [LOG-01] RN08 — Conclusão de Meta somente quando TODAS as tarefas estão concluídas

**Arquivos:** [`TarefaService.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Services/Implementations/TarefaService.cs) · [`MetaService.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Services/Implementations/MetaService.cs) · [`IMetaRepository.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Infra/Repositories/Interfaces/IMetaRepository.cs)

**Princípio SOLID:** *Single Responsibility* — a lógica de conclusão de meta deve estar centralizada no `MetaService`, não replicada no `TarefaService`.

**Passo 1 — Adicionar método ao `IMetaRepository`:**
```csharp
public interface IMetaRepository
{
    // ... métodos existentes ...

    /// <summary>Retorna todas as tarefas vinculadas a uma meta específica.</summary>
    Task<List<Tarefa>> ObterTarefasDaMetaAsync(int metaId);
}
```

**Passo 2 — Implementar em `MetaRepository`:**
```csharp
public async Task<List<Tarefa>> ObterTarefasDaMetaAsync(int metaId)
{
    // Uma meta está vinculada a UMA tarefa (conforme o modelo atual)
    // Se no futuro Meta tiver N tarefas, expandir aqui
    return await _context.Metas
        .AsNoTracking()
        .Where(m => m.Id == metaId)
        .Select(m => m.Tarefa)
        .ToListAsync();
}
```

**Passo 3 — Corrigir `MetaService.VerificarConclusaoMetaAsync`** (método principal da RN08):
```csharp
public async Task<int> VerificarConclusaoMetaAsync(int tarefaId)
{
    // RN08: só marca a meta como Concluída se TODAS as tarefas vinculadas estiverem concluídas
    var metas = await _metaRepository.ObterMetasPorTarefaIdAsync(tarefaId);
    var metasConcluidas = 0;

    foreach (var meta in metas)
    {
        // Busca TODAS as tarefas vinculadas à esta meta
        var tarefasDaMeta = await _metaRepository.ObterTarefasDaMetaAsync(meta.Id);

        // Só conclui se TODAS estiverem concluídas
        var todasConcluidas = tarefasDaMeta.All(t => t.Status == Status.Concluida);

        if (todasConcluidas && meta.Concluida != true)
        {
            meta.Concluida = true;
            await _metaRepository.AtualizarMetaAsync(meta);
            metasConcluidas++;

            _logger.LogInformation(
                "Meta {MetaId} ('{Nome}') concluída automaticamente — todas as tarefas vinculadas estão concluídas.",
                meta.Id, meta.Nome);
        }
    }

    return metasConcluidas;
}
```

**Passo 4 — Corrigir `TarefaService.ConcluirTarefaAsync`** — delegar ao `MetaService` (SRP):
```csharp
// TarefaService precisa injetar IMetaService (não IMetaRepository diretamente)
// Adicionar ao construtor:
private readonly IMetaService _metaService;

public async Task<DetalhesTarefaDTO> ConcluirTarefaAsync(int id, int solicitanteId)
{
    // ... validações e atualização de status existentes ...

    tarefa.Status = Status.Concluida;
    await _tarefaRepository.AtualizarTarefaAsync(tarefa);

    _logger.LogInformation("Auditoria: Tarefa concluída. TarefaId: {TarefaId}, ConcluidaPor: {ConcluidaPor}",
        id, solicitanteId);

    // Delega a verificação de metas ao MetaService (SRP — cada serviço cuida do seu domínio)
    await _metaService.VerificarConclusaoMetaAsync(id);

    var tarefaAtualizada = await _tarefaRepository.ObterTarefaPorIdAsync(id);
    return MapToDetalhesDTO(tarefaAtualizada ?? tarefa);
}
```

> ⚠️ **Atenção a dependência circular:** `TarefaService` injeta `IMetaService` e `MetaService` injeta `ITarefaRepository`. Isso é aceitável pois não há ciclo de interface — apenas de dados. Se preferir, crie um `IMetaConclusaoService` separado apenas com `VerificarConclusaoMetaAsync` para isolar melhor (ISP).

---

### [LOG-05] `Sugestao` — Adicionar Status e Persistir Aprovação/Rejeição

**Princípios SOLID:**
- *Open/Closed*: adicionar o enum permite novos estados no futuro sem alterar a lógica de negócio existente.
- *Single Responsibility*: o estado da sugestão é responsabilidade da entidade `Sugestao`.

**Passo 1 — Criar enum `StatusSugestao`** em `Entities/ENUMS/StatusSugestao.cs`:
```csharp
namespace Cortex.Entities
{
    public enum StatusSugestao
    {
        Pendente,
        Aprovada,
        Rejeitada
    }
}
```

**Passo 2 — Adicionar `Status` à entidade `Sugestao`:**
```csharp
public class Sugestao
{
    public int Id { get; set; }
    public required string Descricao { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public StatusSugestao Status { get; set; } = StatusSugestao.Pendente; // novo campo
    public int TarefaId { get; set; }
    public int UsuarioId { get; set; }
    public Tarefa Tarefa { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
}
```

**Passo 3 — Registrar enum no PostgreSQL** em `Program.cs`:
```csharp
npgsqlOptions.MapEnum<StatusSugestao>("StatusSugestao");
```

**Passo 4 — Registrar no `OnModelCreating`** em `CortexDbContext.cs`:
```csharp
modelBuilder.HasPostgresEnum<StatusSugestao>();
```

**Passo 5 — Adicionar ao Fluent API** em `Infra/Configuration/SugestaoConfiguration.cs`:
```csharp
builder.Property(s => s.Status)
    .HasColumnType("\"StatusSugestao\"")
    .HasDefaultValue(StatusSugestao.Pendente)
    .IsRequired();
```

**Passo 6 — Corrigir `SugestaoService`** — persistir o estado:
```csharp
public async Task<ConsultaSugestaoDTO> AprovarSugestaoAsync(int sugestaoId, int solicitanteId)
{
    var sugestao = await _sugestaoRepository.ObterSugestaoPorIdAsync(sugestaoId)
        ?? throw new KeyNotFoundException("Sugestão não encontrada.");

    if (sugestao.Tarefa.CriadorId != solicitanteId)
        throw new ForbiddenException("Apenas o criador da tarefa pode aprovar sugestões.");

    // Impedir re-aprovação ou aprovação de sugestão já rejeitada
    if (sugestao.Status != StatusSugestao.Pendente)
        throw new InvalidOperationException($"Esta sugestão já foi {sugestao.Status.ToString().ToLower()} e não pode ser alterada.");

    // Aplica as alterações na tarefa principal (RN14)
    sugestao.Tarefa.Descricao = string.IsNullOrWhiteSpace(sugestao.Tarefa.Descricao)
        ? $"[Sugestão #{sugestao.Id} Aprovada]: {sugestao.Descricao}"
        : $"{sugestao.Tarefa.Descricao}\n\n[Sugestão #{sugestao.Id} Aprovada]: {sugestao.Descricao}";

    await _tarefaRepository.AtualizarTarefaAsync(sugestao.Tarefa);

    // Persiste o novo status da sugestão
    sugestao.Status = StatusSugestao.Aprovada;
    await _sugestaoRepository.AtualizarSugestaoAsync(sugestao);

    _logger.LogInformation("Auditoria: Sugestão aprovada. SugestaoId: {Id}, TarefaId: {TId}, SolicitanteId: {Sid}",
        sugestao.Id, sugestao.TarefaId, solicitanteId);

    return MapToConsultaDTO(sugestao);
}

public async Task<ConsultaSugestaoDTO> RejeitarSugestaoAsync(int sugestaoId, int solicitanteId)
{
    var sugestao = await _sugestaoRepository.ObterSugestaoPorIdAsync(sugestaoId)
        ?? throw new KeyNotFoundException("Sugestão não encontrada.");

    if (sugestao.Tarefa.CriadorId != solicitanteId)
        throw new ForbiddenException("Apenas o criador da tarefa pode rejeitar sugestões.");

    if (sugestao.Status != StatusSugestao.Pendente)
        throw new InvalidOperationException($"Esta sugestão já foi {sugestao.Status.ToString().ToLower()} e não pode ser alterada.");

    // Persiste o status de rejeição
    sugestao.Status = StatusSugestao.Rejeitada;
    await _sugestaoRepository.AtualizarSugestaoAsync(sugestao);

    _logger.LogInformation("Auditoria: Sugestão rejeitada. SugestaoId: {Id}, TarefaId: {TId}, SolicitanteId: {Sid}",
        sugestao.Id, sugestao.TarefaId, solicitanteId);

    return MapToConsultaDTO(sugestao);
}
```

**Passo 7 — Adicionar `Status` ao DTO de resposta** em `ConsultaSugestaoDTO`:
```csharp
public StatusSugestao Status { get; set; }
```

**Passo 8 — Gerar migration:**
```bash
dotnet ef migrations add AddStatusSugestao
dotnet ef database update
```

---

## 🟠 BLOCO 2 — CURTO PRAZO (próxima sprint)

---

### [SEC-06] Unificar Mensagem de Erro no Login

**Arquivo:** [`AuthController.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Controllers/AuthController.cs#L42-L70)

**Princípio SOLID:** *Single Responsibility* — a controller não deve ter dois caminhos de falha com mensagens distintas para o mesmo cenário de erro.

```csharp
[HttpPost("login")]
[AllowAnonymous]
[EnableRateLimiting("login-policy")]
public async Task<IActionResult> Login([FromBody] DadosLoginDTO dto)
{
    var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

    // Uma única mensagem para qualquer falha de credencial (previne user enumeration)
    const string mensagemErroGenerico = "E-mail ou senha incorretos.";

    var credenciaisValidas = await _usuarioService.ValidarSenhaAsync(dto.Email, dto.Senha);
    if (!credenciaisValidas)
    {
        _logger.LogWarning("Auditoria: Login falhado. Email: {Email}, IP: {IP}", dto.Email, clientIp);
        return Unauthorized(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Não autorizado",
            Detail = mensagemErroGenerico,
            Instance = HttpContext.Request.Path
        });
    }

    var usuario = await _usuarioRepository.ObterUsuarioPorEmailAsync(dto.Email);
    if (usuario == null) // Caso defensivo — não deveria ocorrer se ValidarSenhaAsync passou
    {
        _logger.LogError("Inconsistência: ValidarSenhaAsync retornou true mas usuário não encontrado. Email: {Email}", dto.Email);
        return Unauthorized(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Não autorizado",
            Detail = mensagemErroGenerico, // mesma mensagem
            Instance = HttpContext.Request.Path
        });
    }

    // ... restante permanece igual
}
```

---

### [SEC-07] Upload de Arquivos — Validação de Tipo

**Princípio SOLID:** *Single Responsibility* + *Open/Closed* — criar um serviço dedicado de validação de arquivo que pode ser estendido sem modificar os serviços que o usam.

**Passo 1 — Criar interface `IArquivoValidacaoService`** em `Services/Interfaces/IArquivoValidacaoService.cs`:
```csharp
namespace Cortex.Services.Interfaces
{
    public interface IArquivoValidacaoService
    {
        /// <summary>Valida extensão, MIME type e tamanho do arquivo. Lança ArgumentException se inválido.</summary>
        void ValidarArquivo(IFormFile arquivo, string[] extensoesPermitidas, long tamanhoMaximoBytes);

        /// <summary>Validação específica para imagens de perfil.</summary>
        void ValidarImagemPerfil(IFormFile arquivo);

        /// <summary>Validação genérica para anexos de tarefas.</summary>
        void ValidarAnexoTarefa(IFormFile arquivo);
    }
}
```

**Passo 2 — Implementar `ArquivoValidacaoService`** em `Services/Implementations/ArquivoValidacaoService.cs`:
```csharp
namespace Cortex.Services.Implementations
{
    public class ArquivoValidacaoService : IArquivoValidacaoService
    {
        // Magic bytes para validação real do conteúdo (não confia apenas na extensão)
        private static readonly Dictionary<string, byte[][]> MagicBytes = new()
        {
            { ".jpg",  [new byte[] { 0xFF, 0xD8, 0xFF }] },
            { ".jpeg", [new byte[] { 0xFF, 0xD8, 0xFF }] },
            { ".png",  [new byte[] { 0x89, 0x50, 0x4E, 0x47 }] },
            { ".webp", [new byte[] { 0x52, 0x49, 0x46, 0x46 }] },
            { ".pdf",  [new byte[] { 0x25, 0x50, 0x44, 0x46 }] },
            { ".docx", [new byte[] { 0x50, 0x4B, 0x03, 0x04 }] },
            { ".xlsx", [new byte[] { 0x50, 0x4B, 0x03, 0x04 }] },
        };

        private static readonly string[] ExtensoesFotoPermitidas = [".jpg", ".jpeg", ".png", ".webp"];
        private static readonly string[] ExtensoesAnexoPermitidas = [".jpg", ".jpeg", ".png", ".webp", ".pdf", ".docx", ".xlsx", ".txt"];
        private const long TamanhoMaximoFoto = 5 * 1024 * 1024;   // 5 MB
        private const long TamanhoMaximoAnexo = 20 * 1024 * 1024; // 20 MB

        public void ValidarImagemPerfil(IFormFile arquivo)
            => ValidarArquivo(arquivo, ExtensoesFotoPermitidas, TamanhoMaximoFoto);

        public void ValidarAnexoTarefa(IFormFile arquivo)
            => ValidarArquivo(arquivo, ExtensoesAnexoPermitidas, TamanhoMaximoAnexo);

        public void ValidarArquivo(IFormFile arquivo, string[] extensoesPermitidas, long tamanhoMaximoBytes)
        {
            if (arquivo == null || arquivo.Length == 0)
                throw new ArgumentException("O arquivo é inválido ou está vazio.");

            if (arquivo.Length > tamanhoMaximoBytes)
                throw new ArgumentException($"O arquivo excede o tamanho máximo permitido de {tamanhoMaximoBytes / 1024 / 1024}MB.");

            // Sanitiza o nome original — impede path traversal
            var nomeOriginalSanitizado = Path.GetFileName(arquivo.FileName);
            var extensao = Path.GetExtension(nomeOriginalSanitizado).ToLowerInvariant();

            if (!extensoesPermitidas.Contains(extensao))
                throw new ArgumentException($"Tipo de arquivo não permitido. Extensões aceitas: {string.Join(", ", extensoesPermitidas)}");

            // Valida magic bytes (conteúdo real do arquivo)
            if (MagicBytes.TryGetValue(extensao, out var assinaturasValidas))
            {
                using var stream = arquivo.OpenReadStream();
                var header = new byte[8];
                var bytesLidos = stream.Read(header, 0, header.Length);

                var assinaturaValida = assinaturasValidas.Any(assinatura =>
                    bytesLidos >= assinatura.Length &&
                    header.Take(assinatura.Length).SequenceEqual(assinatura));

                if (!assinaturaValida)
                    throw new ArgumentException("O conteúdo do arquivo não corresponde à extensão informada.");
            }
        }
    }
}
```

**Passo 3 — Registrar no `Program.cs`:**
```csharp
builder.Services.AddScoped<IArquivoValidacaoService, ArquivoValidacaoService>();
```

**Passo 4 — Injetar e usar em `TarefaService` e `AnexoService`:**
```csharp
// TarefaService — no método SalvarAnexoFisicoAsync:
private async Task<Anexo> SalvarAnexoFisicoAsync(IFormFile arquivo)
{
    _arquivoValidacaoService.ValidarAnexoTarefa(arquivo); // ← adicionar esta linha

    var extensao = Path.GetExtension(Path.GetFileName(arquivo.FileName)).ToLowerInvariant();
    var nomeArquivo = $"{Guid.NewGuid()}{extensao}";
    // ... restante permanece igual
}

// UsuarioService — no método SalvarFotoPerfilAsync:
private async Task<Anexo> SalvarFotoPerfilAsync(IFormFile arquivo)
{
    _arquivoValidacaoService.ValidarImagemPerfil(arquivo); // ← substituir validação manual
    // ... remover a validação manual existente e manter apenas o salvamento
}
```

---

### [LOG-02] Autorização Multi-Equipe — Refatoração Completa

**Princípios SOLID:**
- *Single Responsibility*: `IEquipeAuthorizationService` é responsável exclusivamente por decidir permissões no contexto de equipe.
- *Dependency Inversion*: Services dependem da abstração `IEquipeAuthorizationService`.
- *Interface Segregation*: interface coesa com apenas os métodos de autorização necessários.

**Passo 1 — Adicionar método ao `IMembroEquipeRepository`:**
```csharp
public interface IMembroEquipeRepository
{
    // ... métodos existentes ...

    /// <summary>
    /// Retorna os membros do usuário nas equipes informadas.
    /// Usado para verificar cargo em múltiplas equipes de uma só vez (evita N+1).
    /// </summary>
    Task<List<MembroEquipe>> ObterMembrosPorUsuarioEEquipesAsync(int usuarioId, IEnumerable<int> equipeIds);
}
```

**Passo 2 — Implementar em `MembroEquipeRepository`:**
```csharp
public async Task<List<MembroEquipe>> ObterMembrosPorUsuarioEEquipesAsync(
    int usuarioId, IEnumerable<int> equipeIds)
{
    return await _context.MembrosEquipe
        .AsNoTracking()
        .Where(m => m.UsuarioId == usuarioId && equipeIds.Contains(m.EquipeId))
        .ToListAsync();
}
```

**Passo 3 — Criar `IEquipeAuthorizationService`** em `Services/Interfaces/IEquipeAuthorizationService.cs`:
```csharp
namespace Cortex.Services.Interfaces
{
    /// <summary>
    /// Centraliza a lógica de autorização baseada em cargo dentro de equipes.
    /// Suporta usuários com múltiplos cargos em equipes distintas.
    /// </summary>
    public interface IEquipeAuthorizationService
    {
        /// <summary>Verifica se o usuário é Dono ou Admin na equipe especificada.</summary>
        Task<bool> EhAdminOuDonoDaEquipeAsync(int usuarioId, int equipeId);

        /// <summary>Verifica se o usuário é Dono na equipe especificada (RN03).</summary>
        Task<bool> EhDonoDaEquipeAsync(int usuarioId, int equipeId);

        /// <summary>
        /// Verifica se o usuário é Dono ou Admin em pelo menos uma das equipes
        /// cujos membros são responsáveis pela tarefa informada.
        /// </summary>
        Task<bool> EhAdminOuDonoDaTarefaAsync(int usuarioId, Tarefa tarefa);

        /// <summary>Lança ForbiddenException se o usuário não for Admin/Dono da equipe.</summary>
        Task ExigirAdminOuDonoDaEquipeAsync(int usuarioId, int equipeId);

        /// <summary>Lança ForbiddenException se o usuário não for Dono da equipe.</summary>
        Task ExigirDonoDaEquipeAsync(int usuarioId, int equipeId);
    }
}
```

**Passo 4 — Implementar `EquipeAuthorizationService`** em `Services/Implementations/EquipeAuthorizationService.cs`:
```csharp
using Cortex.Entities;
using Cortex.Infra.Exceptions;
using Cortex.Repositories.Interface;
using Cortex.Services.Interfaces;

namespace Cortex.Services.Implementations
{
    public class EquipeAuthorizationService : IEquipeAuthorizationService
    {
        private readonly IMembroEquipeRepository _membroEquipeRepository;

        public EquipeAuthorizationService(IMembroEquipeRepository membroEquipeRepository)
        {
            _membroEquipeRepository = membroEquipeRepository;
        }

        public async Task<bool> EhAdminOuDonoDaEquipeAsync(int usuarioId, int equipeId)
        {
            var membro = await _membroEquipeRepository.ObterMembroPorEquipeEUsuarioAsync(equipeId, usuarioId);
            return membro != null && (membro.Cargo == Cargo.Dono || membro.Cargo == Cargo.Admin);
        }

        public async Task<bool> EhDonoDaEquipeAsync(int usuarioId, int equipeId)
        {
            var membro = await _membroEquipeRepository.ObterMembroPorEquipeEUsuarioAsync(equipeId, usuarioId);
            return membro?.Cargo == Cargo.Dono;
        }

        public async Task<bool> EhAdminOuDonoDaTarefaAsync(int usuarioId, Tarefa tarefa)
        {
            // Encontra as equipes dos responsáveis pela tarefa
            var responsaveisIds = tarefa.ResponsaveisTarefas
                .Select(rt => rt.UsuarioId)
                .Distinct()
                .ToList();

            if (!responsaveisIds.Any())
                return false;

            // Busca as equipes em que os responsáveis estão como membros
            var equipesDosResponsaveis = await _membroEquipeRepository
                .ObterMembrosPorUsuarioEEquipesIdsAsync(responsaveisIds);

            var equipeIds = equipesDosResponsaveis.Select(m => m.EquipeId).Distinct().ToList();

            if (!equipeIds.Any())
                return false;

            // Verifica se o solicitante é Admin/Dono em alguma dessas equipes
            var membrosDoSolicitante = await _membroEquipeRepository
                .ObterMembrosPorUsuarioEEquipesAsync(usuarioId, equipeIds);

            return membrosDoSolicitante.Any(m => m.Cargo == Cargo.Dono || m.Cargo == Cargo.Admin);
        }

        public async Task ExigirAdminOuDonoDaEquipeAsync(int usuarioId, int equipeId)
        {
            if (!await EhAdminOuDonoDaEquipeAsync(usuarioId, equipeId))
                throw new ForbiddenException("Apenas o Dono ou Administrador da equipe possuem permissão para esta operação.");
        }

        public async Task ExigirDonoDaEquipeAsync(int usuarioId, int equipeId)
        {
            if (!await EhDonoDaEquipeAsync(usuarioId, equipeId))
                throw new ForbiddenException("Apenas o Dono da equipe possui permissão para esta operação.");
        }
    }
}
```

> O método `ObterMembrosPorUsuarioEEquipesIdsAsync` (variante) busca membros de um conjunto de usuários em suas equipes — adicionar também ao repositório.

**Passo 5 — Registrar no `Program.cs`:**
```csharp
builder.Services.AddScoped<IEquipeAuthorizationService, EquipeAuthorizationService>();
```

**Passo 6 — Atualizar `TarefaService`** (exemplo representativo — aplicar o mesmo em `MetaService`, `SugestaoService`, `TicketReporteService`):
```csharp
// Injetar IEquipeAuthorizationService no construtor
private readonly IEquipeAuthorizationService _authService;

public async Task<DetalhesTarefaDTO> AtualizarTarefaAsync(int id, AtualizarTarefaDTO dto, int solicitanteId)
{
    var tarefa = await _tarefaRepository.ObterTarefaPorIdAsync(id)
        ?? throw new KeyNotFoundException("Tarefa não encontrada.");

    var ehCriador = tarefa.CriadorId == solicitanteId;

    // Verificação contextual — cargo na equipe DA TAREFA, não em qualquer equipe
    var ehAdminOuDono = await _authService.EhAdminOuDonoDaTarefaAsync(solicitanteId, tarefa);

    if (!ehCriador && !ehAdminOuDono)
        throw new ForbiddenException("Apenas o criador da tarefa ou administradores da equipe podem editá-la.");

    // ... restante da lógica permanece igual
}
```

**Passo 7 — Atualizar `EquipeService` e `MembroEquipeService`** — substituir verificações manuais pelas do `IEquipeAuthorizationService`:
```csharp
// EquipeService.AtualizarEquipeAsync — antes:
var membro = await _membroEquipeRepository.ObterMembroPorEquipeEUsuarioAsync(id, solicitanteId);
if (membro == null || (membro.Cargo != Cargo.Dono && membro.Cargo != Cargo.Admin))
    throw new ForbiddenException("...");

// EquipeService.AtualizarEquipeAsync — depois:
await _authService.ExigirAdminOuDonoDaEquipeAsync(solicitanteId, id);
```

**Passo 8 — `TokenService`** — remover claims de cargo e equipeId ou manter apenas como metadado:
```csharp
// Remover ou comentar estas linhas do TokenService:
// claims.Add(new Claim("cargo", cargo));
// claims.Add(new Claim(ClaimTypes.Role, cargo));
// claims.Add(new Claim("equipeId", equipeId.Value.ToString()));

// O token passa a conter apenas dados de identidade:
// sub, email, name, jti
```

**Passo 9 — `UsuarioController`** — substituir `[Authorize(Roles)]` por verificação no service:
```csharp
// Antes:
[Authorize(Roles = "Dono,Admin")]
public async Task<IActionResult> Listar() { ... }

// Depois — a controller autoriza qualquer usuário autenticado
// e o service decide o que retornar baseado no contexto
[Authorize]
public async Task<IActionResult> Listar()
{
    var currentUserId = ObterUsuarioIdAutenticado();
    var usuarios = await _usuarioService.ListarUsuariosAsync(currentUserId);
    return Ok(usuarios);
}
```

---

### [LOG-03] `AtualizarTarefaDTO` — Tipos Nullable para Patch Seguro

**Arquivo:** `DTOs/Request/AtualizarTarefaDTO.cs`

**Princípio SOLID:** *Open/Closed* — o DTO deve suportar atualizações parciais sem forçar o cliente a enviar todos os campos.

```csharp
namespace Cortex.DTOs.Request
{
    public class AtualizarTarefaDTO
    {
        public string? Titulo { get; set; }
        public string? Descricao { get; set; }
        public DateTime? DataVencimento { get; set; }   // nullable — não sobrescreve se null
        public int? Pontuacao { get; set; }
        public Prioridade? Prioridade { get; set; }     // nullable
        public List<IFormFile>? Anexos { get; set; }
    }
}
```

**Atualizar `TarefaService.AtualizarTarefaAsync`:**
```csharp
// Aplicar campos somente se enviados (patch semântico)
if (!string.IsNullOrWhiteSpace(dto.Titulo))
    tarefa.Titulo = dto.Titulo;

if (dto.Descricao != null) // null = não alterar; "" = limpar
    tarefa.Descricao = dto.Descricao;

if (dto.DataVencimento.HasValue)
{
    var dataUtc = dto.DataVencimento.Value.Kind == DateTimeKind.Utc
        ? dto.DataVencimento.Value
        : dto.DataVencimento.Value.ToUniversalTime(); // usar ToUniversalTime() ao invés de SpecifyKind
    tarefa.DataVencimento = dataUtc;
}

if (dto.Pontuacao.HasValue)
    tarefa.Pontuacao = dto.Pontuacao.Value;

if (dto.Prioridade.HasValue)
    tarefa.Prioridade = dto.Prioridade.Value;
```

---

### [LOG-04] `DeletarUsuarioAsync` — Passar `solicitanteId` para Auditoria

**Arquivo:** [`UsuarioService.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Services/Implementations/UsuarioService.cs) · [`IUsuarioService.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Services/Interfaces/IUsuarioService.cs)

**Passo 1 — Atualizar a interface `IUsuarioService`:**
```csharp
// Antes:
Task<bool> DeletarUsuarioAsync(int id);

// Depois:
Task<bool> DeletarUsuarioAsync(int id, int solicitanteId);
```

**Passo 2 — Atualizar a implementação em `UsuarioService`:**
```csharp
public async Task<bool> DeletarUsuarioAsync(int id, int solicitanteId)
{
    var usuario = await _usuarioRepository.ObterUsuarioPorIdAsync(id);
    if (usuario == null)
        return false;

    // RN05: verificações de tarefas em andamento...

    var removido = await _usuarioRepository.RemoverUsuarioAsync(id);
    if (removido)
    {
        _logger.LogWarning(
            "Auditoria: Exclusão de registro. Entidade: {Entidade}, Id: {Id}, DeletadoPor: {DeletadoPor}",
            "Usuario", id, solicitanteId); // ← solicitanteId correto
    }

    return removido;
}
```

**Passo 3 — Atualizar `UsuarioController`:**
```csharp
public async Task<IActionResult> Deletar(int id)
{
    var currentUserId = ObterUsuarioIdAutenticado();
    // ... verificação de permissão ...

    var deletado = await _usuarioService.DeletarUsuarioAsync(id, currentUserId); // ← passar currentUserId
    // ...
}
```

---

### [ARQ-01] Eliminar `CortexDbContext` direto em Services

**Princípio SOLID:** *Dependency Inversion* — Services dependem de abstrações (interfaces de repositório), não de implementações concretas de banco.

**Passo 1 — Criar `IPreferenciaNotificacaoRepository`** em `Infra/Repositories/Interfaces/IPreferenciaNotificacaoRepository.cs`:
```csharp
using Cortex.Entities;

namespace Cortex.Repositories.Interface
{
    public interface IPreferenciaNotificacaoRepository
    {
        Task<PreferenciaNotificacao?> ObterPorUsuarioECanalAsync(int usuarioId, CanalNotificacao canal);
        Task<List<PreferenciaNotificacao>> ObterPorUsuarioAsync(int usuarioId);
        Task<PreferenciaNotificacao> AdicionarAsync(PreferenciaNotificacao preferencia);
        Task<PreferenciaNotificacao> AtualizarAsync(PreferenciaNotificacao preferencia);
    }
}
```

**Passo 2 — Implementar `PreferenciaNotificacaoRepository`** em `Infra/Repositories/Implementations/PreferenciaNotificacaoRepository.cs`:
```csharp
using Cortex.Entities;
using Cortex.Infra.Persistence;
using Cortex.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace Cortex.Repositories.Implementations
{
    public class PreferenciaNotificacaoRepository : IPreferenciaNotificacaoRepository
    {
        private readonly CortexDbContext _context;

        public PreferenciaNotificacaoRepository(CortexDbContext context)
        {
            _context = context;
        }

        public async Task<PreferenciaNotificacao?> ObterPorUsuarioECanalAsync(int usuarioId, CanalNotificacao canal)
            => await _context.PreferenciasNotificacao
                .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId && p.Canal == canal);

        public async Task<List<PreferenciaNotificacao>> ObterPorUsuarioAsync(int usuarioId)
            => await _context.PreferenciasNotificacao
                .Where(p => p.UsuarioId == usuarioId)
                .ToListAsync();

        public async Task<PreferenciaNotificacao> AdicionarAsync(PreferenciaNotificacao preferencia)
        {
            _context.PreferenciasNotificacao.Add(preferencia);
            await _context.SaveChangesAsync();
            return preferencia;
        }

        public async Task<PreferenciaNotificacao> AtualizarAsync(PreferenciaNotificacao preferencia)
        {
            _context.PreferenciasNotificacao.Update(preferencia);
            await _context.SaveChangesAsync();
            return preferencia;
        }
    }
}
```

**Criar também `IRelatorioRepository`** para as queries do `RelatorioService`:
```csharp
public interface IRelatorioRepository
{
    Task<List<Tarefa>> ObterTarefasParaRelatorioAsync(
        int? solicitanteId,
        bool ehAdminOuDono,
        FiltroRelatorioDTO filtro);

    Task<List<int>> ObterIdsMembrosDaEquipeAsync(int equipeId);

    Task<List<Tarefa>> ObterTarefasDaEquipeAsync(int equipeId, FiltroRelatorioDTO filtro);
}
```

**Passo 3 — Registrar no `Program.cs`:**
```csharp
builder.Services.AddScoped<IPreferenciaNotificacaoRepository, PreferenciaNotificacaoRepository>();
builder.Services.AddScoped<IRelatorioRepository, RelatorioRepository>();
```

**Passo 4 — Atualizar `NotificacaoService`** — substituir `CortexDbContext` por `IPreferenciaNotificacaoRepository`:
```csharp
// Remover:
private readonly CortexDbContext _context;

// Adicionar:
private readonly IPreferenciaNotificacaoRepository _preferenciaRepository;

// Atualizar ConfigurarPreferenciasAsync:
var preferencia = await _preferenciaRepository.ObterPorUsuarioECanalAsync(usuarioId, dto.Canal);

if (preferencia == null)
{
    preferencia = new PreferenciaNotificacao { ... };
    await _preferenciaRepository.AdicionarAsync(preferencia);
}
else
{
    preferencia.AntecedenciaHoras = dto.AntecedenciaHoras;
    // ... atualizar campos
    await _preferenciaRepository.AtualizarAsync(preferencia);
}
```

---

### [ARQ-02] Padronizar Rotas nos Controllers

**Arquivos:** [`SugestaoController.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Controllers/SugestaoController.cs) · [`TicketReporteController.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Controllers/TicketReporteController.cs) · [`AnexoController.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Controllers/AnexoController.cs) · [`MetaController.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Controllers/MetaController.cs)

**Princípio SOLID:** Consistência estrutural — todos os controllers devem seguir o mesmo padrão de roteamento.

Esses controllers definem rotas absolutas nas actions em vez de usar rota base na classe. Existem duas abordagens válidas — escolha uma e aplique a todos:

**Opção A (recomendada) — Manter rotas absolutas nas actions com `[Route("")]` na classe:**
```csharp
[ApiController]
[Route("")] // rota base vazia — as actions definem caminhos absolutos
[Authorize]
public class SugestaoController : ControllerBase
{
    [HttpPost("api/tarefas/{id:int}/sugestoes")]
    public async Task<IActionResult> Criar(...) { }

    [HttpGet("api/tarefas/{id:int}/sugestoes")]
    public async Task<IActionResult> ListarPorTarefa(...) { }

    [HttpPatch("api/sugestoes/{id:int}/aprovar")]
    public async Task<IActionResult> Aprovar(...) { }

    [HttpPatch("api/sugestoes/{id:int}/rejeitar")]
    public async Task<IActionResult> Rejeitar(...) { }
}
```

**Mesma correção para `TicketReporteController`, `AnexoController`, `MetaController`.**

---

## 🟡 BLOCO 3 — MÉDIO PRAZO (refinamento)

---

### [ARQ-03] `BaseController` — Eliminar Duplicação do `ObterUsuarioIdAutenticado`

**Princípio SOLID:** *Don't Repeat Yourself* (extensão do SRP) — lógica comum de extração de identity deve estar em um único lugar.

**Criar `BaseController`** em `Controllers/BaseController.cs`:
```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Cortex.Controllers
{
    /// <summary>
    /// Controller base com utilitários comuns de identidade e resposta HTTP.
    /// Todos os controllers do Cortex herdam desta classe.
    /// </summary>
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        /// <summary>
        /// Extrai o ID do usuário autenticado a partir das claims do JWT.
        /// Lança UnauthorizedAccessException se o claim não existir ou for inválido.
        /// </summary>
        protected int ObterUsuarioIdAutenticado()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (int.TryParse(claim, out var id))
                return id;

            throw new UnauthorizedAccessException("Usuário não autenticado ou identificador inválido no token.");
        }

        /// <summary>Retorna ProblemDetails padronizado para 404.</summary>
        protected IActionResult NaoEncontrado(string detalhe) => NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Recurso não encontrado",
            Detail = detalhe,
            Instance = HttpContext.Request.Path
        });
    }
}
```

**Atualizar todos os controllers para herdar de `BaseController`:**
```csharp
// Antes:
public class TarefaController : ControllerBase

// Depois:
public class TarefaController : BaseController
// Remover o método ObterUsuarioIdAutenticado() privado de cada controller
```

---

### [ARQ-04] Remover `TesteController`

```bash
# Simplesmente deletar o arquivo:
del "Controllers\TesteController.cs"
```

---

### [LOG-07] Controle de "Alerta Já Enviado" no `NotificacaoService`

**Passo 1 — Adicionar entidade `AlertaEnviado`** em `Entities/AlertaEnviado.cs`:
```csharp
namespace Cortex.Entities
{
    public class AlertaEnviado
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int TarefaId { get; set; }
        public CanalNotificacao Canal { get; set; }
        public DateTime EnviadoEm { get; set; } = DateTime.UtcNow;

        // Navegação
        public Usuario Usuario { get; set; } = null!;
        public Tarefa Tarefa { get; set; } = null!;
    }
}
```

**Passo 2 — Usar no `NotificacaoService.ProcessarAlertasVencimentoAsync`:**
```csharp
// Antes de enviar, verificar se já foi enviado nesta janela
var jaEnviado = await _alertaEnviadoRepository
    .ExisteAlertaEnviadoAsync(usuarioId, tarefa.Id, pref.Canal, agoraUtc.AddHours(-pref.AntecedenciaHoras));

if (!jaEnviado)
{
    var enviado = await EnviarNotificacaoAsync(usuarioId, pref.Canal, assunto, mensagem);
    if (enviado)
    {
        await _alertaEnviadoRepository.RegistrarAsync(usuarioId, tarefa.Id, pref.Canal);
        alertasDisparados++;
    }
}
```

---

### [ARQ-09] Substituir `DateTime.SpecifyKind` por `ToUniversalTime`

**Aplicar em todos os serviços** onde existe o padrão:
```csharp
// Padrão incorreto — apenas muda o Kind sem converter o valor:
DateTime.SpecifyKind(dto.DataVencimento, DateTimeKind.Utc)

// Padrão correto — converte o valor para UTC:
dto.DataVencimento.ToUniversalTime()

// Ou, ainda melhor — usar DateTimeOffset para eliminar ambiguidade:
// No DTO: public DateTimeOffset DataVencimento { get; set; }
// No Service: var dataUtc = dto.DataVencimento.UtcDateTime;
```

**Arquivos afetados:**
- [`TarefaService.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Services/Implementations/TarefaService.cs) — linhas 43-45, 123-125
- [`MetaService.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Services/Implementations/MetaService.cs) — linhas 41-43, 142-144
- [`RelatorioService.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Services/Implementations/RelatorioService.cs) — linhas 81-83, 89-91

---

### [ARQ-10] Validar Nova Senha Diferente da Atual em `AlterarSenha`

**Arquivo:** [`UsuarioService.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Services/Implementations/UsuarioService.cs#L159)

```csharp
public async Task<bool> AlterarSenhaAsync(int usuarioId, string senhaAtual, string novaSenha)
{
    var usuario = await _usuarioRepository.ObterUsuarioPorIdAsync(usuarioId)
        ?? throw new KeyNotFoundException("Usuário não encontrado.");

    if (!_passwordService.VerifyPassword(senhaAtual, usuario.SenhaHash))
        throw new UnauthorizedAccessException("Senha atual incorreta.");

    // Adicionar: verificar se nova senha é diferente da atual
    if (_passwordService.VerifyPassword(novaSenha, usuario.SenhaHash))
        throw new ArgumentException("A nova senha não pode ser igual à senha atual.");

    var (isValid, errorMessage) = _passwordService.ValidatePassword(novaSenha);
    if (!isValid)
        throw new ArgumentException(errorMessage);

    usuario.SenhaHash = _passwordService.HashPassword(novaSenha);
    await _usuarioRepository.AtualizarUsuarioAsync(usuario);

    return true;
}
```

---

### [ARQ-06] Traduzir Mensagens do `PasswordService` para Português

**Arquivo:** [`PasswordService.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Services/Implementations/PasswordService.cs)

```csharp
public (bool IsValid, string? ErrorMessage) ValidatePassword(string password)
{
    if (string.IsNullOrWhiteSpace(password))
        return (false, "A senha é obrigatória.");

    if (password.Length < MinPasswordLength)
        return (false, $"A senha deve ter no mínimo {MinPasswordLength} caracteres.");

    if (password.Length > MaxPasswordLength)
        return (false, $"A senha deve ter no máximo {MaxPasswordLength} caracteres.");

    if (!password.Any(char.IsUpper))
        return (false, "A senha deve conter pelo menos uma letra maiúscula.");

    if (!password.Any(char.IsLower))
        return (false, "A senha deve conter pelo menos uma letra minúscula.");

    if (!password.Any(char.IsDigit))
        return (false, "A senha deve conter pelo menos um número.");

    var specialChars = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~";
    if (!password.Any(c => specialChars.Contains(c)))
        return (false, "A senha deve conter pelo menos um caractere especial.");

    return (true, null);
}
```

---

## 📋 Checklist de Execução

### Bloco 1 — Imediato
- [x] **SEC-01** — User Secrets configurados; `appsettings.Development.json` limpo
- [x] **SEC-02+03** — `VerificacaoEmailService` criado; migration gerada e aplicada
- [x] **SEC-04** — `RequireHttpsMetadata` condicional ao ambiente
- [x] **SEC-05** — Pipeline de middleware reordenado; rate limiting para verificação de e-mail
- [x] **LOG-01** — `VerificarConclusaoMetaAsync` verifica todas as tarefas; `TarefaService` delega ao `MetaService`
- [x] **LOG-05** — `StatusSugestao` enum criado; migration gerada; aprovação/rejeição persistida

### Bloco 2 — Curto Prazo
- [x] **SEC-06** — Mensagem de erro de login unificada
- [x] **SEC-07** — `ArquivoValidacaoService` criado e injetado em `TarefaService`, `AnexoService`, `UsuarioService`
- [x] **LOG-02** — `IEquipeAuthorizationService` criado; `TokenService` sem claims de cargo; todos os services atualizados
- [x] **LOG-03** — `AtualizarTarefaDTO` com tipos nullable; `TarefaService` com patch semântico
- [x] **LOG-04** — `DeletarUsuarioAsync(id, solicitanteId)` com assinatura corrigida
- [x] **ARQ-01** — `IPreferenciaNotificacaoRepository` e `IRelatorioRepository` criados; `CortexDbContext` removido dos services
- [x] **ARQ-02** — `[Route("")]` adicionado aos controllers sem rota na classe

### Bloco 3 — Médio Prazo
- [x] **ARQ-03** — `BaseController` criado; todos os controllers herdam dele
- [x] **ARQ-04** — `TesteController` removido
- [x] **LOG-07** — `AlertaEnviado` entity criada; controle de alerta já enviado implementado
- [x] **ARQ-09** — `DateTime.SpecifyKind` substituído por `ToUniversalTime()` em todos os services
- [x] **ARQ-10** — Validação de nova senha diferente da atual em `AlterarSenha`
- [x] **ARQ-06** — Mensagens do `PasswordService` traduzidas para português
