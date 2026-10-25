# 🔍 Análise de Falhas — Cortex Backend

> **Data da Análise:** Outubro de 2026  
> **Escopo:** Segurança · Lógica de Negócio · Estrutura Geral

---

## 📊 Sumário Executivo

| Categoria | Crítico | Alto | Médio | Baixo |
|-----------|---------|------|-------|-------|
| 🔴 Segurança | 3 | 4 | 2 | 1 |
| 🟠 Lógica de Negócio | 2 | 4 | 3 | 2 |
| 🔵 Estrutura / Arquitetura | 0 | 2 | 5 | 4 |
| **Total** | **5** | **10** | **10** | **7** |

---

## 🔴 FALHAS CRÍTICAS DE SEGURANÇA

---

### [SEC-01] 🟠 Credenciais hardcoded — risco na transição para produção

**Arquivo:** [`appsettings.json`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/appsettings.json#L12-L15) · [`appsettings.Development.json`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/appsettings.Development.json#L3)

**Severidade:** 🟠 ALTO *(aceitável em dev; bloqueante antes de produção)*

**Contexto:** As credenciais expostas estão em um ambiente de desenvolvimento local. Isso é compreensível durante a fase de desenvolvimento ativo. **O risco se torna crítico se este padrão for mantido ao subir para staging ou produção.**

**Problema atual:**
```json
"DefaultConnection": "Host=localhost;Port=5432;Database=Cortex;Username=postgres;Password=221076ls"
"Jwt": { "Secret": "CortexBackendSuperSecretSecurityKey_Min32CharsLong2026!" }
```
Ambos os arquivos estão versionados no Git sem proteção. Se o repositório for tornado público ou compartilhado, as credenciais são expostas.

**Ações recomendadas antes de produção:**
- Criar um `appsettings.Production.json` **nunca versionado** (adicionar ao `.gitignore`)
- Usar variáveis de ambiente (`ASPNETCORE_ConnectionStrings__DefaultConnection`, `ASPNETCORE_Jwt__Secret`) ou um Secret Manager (Azure Key Vault, AWS Secrets Manager, etc.)
- Criar um `appsettings.Development.example.json` com valores fictícios para orientar novos devs — o arquivo `.example.json` já existe no projeto mas não está sendo usado como padrão
- Usar senhas distintas por ambiente (dev ≠ staging ≠ prod)

---

### [SEC-02] 🔴 Código de verificação de e-mail hardcoded

**Arquivo:** [`UsuarioService.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Services/Implementations/UsuarioService.cs#L205) · [`AuthController.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Controllers/AuthController.cs#L135)

**Severidade:** 🔴 CRÍTICO

**Problema:**
```csharp
// UsuarioService.cs linha 205
var codigo = "123456";
```
O código de verificação de e-mail é sempre `"123456"`. Qualquer usuário pode verificar **qualquer e-mail** sem ter acesso à caixa postal, bastando enviar `"123456"`. O log ainda **imprime o código** em texto claro:
```csharp
_logger.LogInformation("Código de verificação gerado para o e-mail {Email}: {Codigo}", email, codigo);
```

**Impacto:** RN13 completamente quebrada — verificação de e-mail é uma segurança ilusória. Logs vazam o código.

**Correção:**
- Gerar código aleatório com `Guid.NewGuid()` ou `RandomNumberGenerator`
- Armazenar hash do código no banco com expiração (ex: 15 minutos)
- Nunca logar o código em texto claro

---

### [SEC-03] 🔴 Verificação de e-mail aceita qualquer código de 6 dígitos

**Arquivo:** [`UsuarioService.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Services/Implementations/UsuarioService.cs#L185-L191)

**Severidade:** 🔴 CRÍTICO

**Problema:**
```csharp
if (string.IsNullOrWhiteSpace(codigo) || codigo.Length != 6)
    throw new ArgumentException("Código de verificação inválido.");

usuario.EmailVerificado = true; // ← marca como verificado sem comparar o código com nenhum valor armazenado!
```
O código recebido nunca é comparado com nada. A validação apenas checa se tem 6 caracteres — qualquer string de 6 caracteres funciona como código de verificação.

**Impacto:** RN13 completamente sem efeito. Qualquer usuário verifica qualquer e-mail com `"aaaaaa"`.

**Correção:**
- Armazenar código/hash no banco (ex: campo `CodigoVerificacao` e `CodigoVerificacaoExpiracao`)
- Comparar o código recebido com o armazenado antes de marcar como verificado
- Invalidar o código após uso

---

### [SEC-04] 🔴 `RequireHttpsMetadata = false` em produção

**Arquivo:** [`Program.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Program.cs#L110)

**Severidade:** 🔴 CRÍTICO

**Problema:**
```csharp
options.RequireHttpsMetadata = false;
```
Desabilita a validação de que tokens JWT só sejam aceitos via HTTPS. Permite ataques man-in-the-middle para interceptar tokens Bearer.

**Impacto:** Em produção, tokens podem ser interceptados e reutilizados.

**Correção:**
```csharp
options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
```

---

### [SEC-05] 🔴 `UseRateLimiter` aplicado após `UseAuthorization`

**Arquivo:** [`Program.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Program.cs#L153-L155)

**Severidade:** 🔴 ALTO

**Problema:**
```csharp
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter(); // ← deve vir ANTES de UseAuthorization
```
O middleware de rate limiting está registrado **depois** de autenticação/autorização. O pipeline ASP.NET Core executa middlewares na ordem de registro. Rate limiting deve ser aplicado antes para bloquear requisições excessivas antes de processá-las.

**Impacto:** Rate limiting não funciona conforme esperado no pipeline — um atacante pode fazer o servidor processar autenticação antes de ser bloqueado.

**Correção:** Mover `app.UseRateLimiter()` para antes de `app.UseAuthentication()`.

---

### [SEC-06] 🔴 Exposição de mensagem diferente para e-mail inexistente no login

**Arquivo:** [`AuthController.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Controllers/AuthController.cs#L57-L70)

**Severidade:** 🔴 ALTO

**Problema:**
```csharp
// Mensagem 1 — credenciais inválidas
Detail = "E-mail ou senha incorretos."

// Mensagem 2 — usuário não encontrado (após ValidarSenhaAsync)
Detail = "Usuário não encontrado."
```
Duas mensagens distintas permitem **user enumeration** — um atacante consegue descobrir quais e-mails estão cadastrados no sistema tentando logins. O segundo bloco é logicamente impossível de atingir (se `ValidarSenhaAsync` retornou `true`, o usuário existe), mas a mensagem diferenciada é um anti-pattern.

**Impacto:** Facilita ataques de enumeração de usuários e direcionamento de ataques de força bruta.

**Correção:** Usar sempre a mesma mensagem genérica: `"E-mail ou senha incorretos."`.

---

### [SEC-07] 🔴 Upload de arquivos sem validação de tipo MIME/conteúdo real

**Arquivo:** [`TarefaService.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Services/Implementations/TarefaService.cs#L242-L268) · [`AnexoService.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Services/Implementations/AnexoService.cs#L153-L188)

**Severidade:** 🔴 ALTO

**Problema:**
```csharp
// SalvarAnexoFisicoAsync em TarefaService — zero validação de tipo
var extensao = Path.GetExtension(arquivo.FileName);
var nomeArquivo = $"{Guid.NewGuid()}{extensao}";
```
O `TarefaService` não valida extensão nem MIME type dos arquivos. Qualquer arquivo pode ser enviado: `.exe`, `.php`, `.sh`, etc. O `UsuarioService` valida extensões para fotos de perfil, mas o `AnexoService` e `TarefaService` não fazem nenhuma verificação.

**Impacto:** Upload de arquivos maliciosos que podem ser executados pelo servidor.

**Correção:**
- Validar lista de extensões e MIME types permitidos em **todos** os pontos de upload
- Usar leitura dos primeiros bytes (magic bytes) para validação real
- Definir tamanho máximo de arquivo
- Sanitizar o `arquivo.FileName` antes de usá-lo como `NomeOriginal`

---

### [SEC-08] 🟠 Silencing de erros ao deletar arquivo físico

**Arquivo:** [`AnexoService.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Services/Implementations/AnexoService.cs#L126)

**Severidade:** 🟠 MÉDIO

**Problema:**
```csharp
try { File.Delete(caminhoArquivo); } catch { /* Ignore file lock */ }
```
Erros de deleção de arquivo são silenciados completamente sem log. Arquivos "órfãos" se acumulam no disco sem qualquer rastro de auditoria.

**Correção:** Logar o erro com `_logger.LogWarning(ex, ...)` ao falhar a deleção.

---

## 🟠 FALHAS DE LÓGICA DE NEGÓCIO

---

### [LOG-01] 🔴 RN08 implementada incorretamente em `TarefaService.ConcluirTarefaAsync`

**Arquivo:** [`TarefaService.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Services/Implementations/TarefaService.cs#L207-L217)

**Severidade:** 🔴 CRÍTICO

**Problema:**
```csharp
// RN08: Conclusão de Metas vinculadas
var metasVinculadas = await _metaRepository.ObterMetasPorTarefaIdAsync(id);
foreach (var meta in metasVinculadas)
{
    meta.Concluida = true; // ← marca TODAS as metas como concluídas sem verificar outras tarefas!
    await _metaRepository.AtualizarMetaAsync(meta);
}
```

A RN08 define: **"só marcar uma meta como Concluída se TODAS as tarefas relacionadas estiverem concluídas"**. O código atual marca a meta como concluída ao concluir **qualquer** tarefa vinculada, ignorando se outras tarefas da mesma meta ainda estão pendentes.

Além disso, existe um `MetaService.VerificarConclusaoMetaAsync` que também tem o mesmo bug — marca todas as metas sem verificar as demais tarefas.

**Impacto:** Metas marcadas prematuramente como concluídas, violando RN08.

**Correção:**
```csharp
// Para cada meta, verificar se TODAS as tarefas vinculadas estão concluídas
foreach (var meta in metasVinculadas)
{
    var todasConcluidas = /* buscar todas as tarefas da meta e checar Status == Concluida */;
    if (todasConcluidas)
    {
        meta.Concluida = true;
        await _metaRepository.AtualizarMetaAsync(meta);
    }
}
```

---

### [LOG-02] 🔴 Modelo de autorização multi-cargo não suportado corretamente

**Arquivos:** [`AuthController.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Controllers/AuthController.cs#L72-L74) · [`TarefaService.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Services/Implementations/TarefaService.cs#L116) · [`MetaService.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Services/Implementations/MetaService.cs#L83) · e outros services

**Severidade:** 🔴 CRÍTICO

**Contexto do domínio:** Um usuário pode pertencer a múltiplas equipes com **cargos diferentes** em cada uma (ex: `Membro` na Equipe A, `Admin` na Equipe B, `Dono` na Equipe C). Ele nunca tem mais de um cargo na mesma equipe.

**Problema 1 — JWT com cargo único:**
```csharp
// AuthController.cs
var equipePrincipal = usuario.Equipes.OrderBy(e => e.Cargo).FirstOrDefault();
var cargo = equipePrincipal?.Cargo.ToString() ?? "Membro"; // apenas UM cargo no token
var equipeId = equipePrincipal?.EquipeId;                  // apenas UMA equipe no token
```
O token captura apenas o cargo de uma equipe. Isso significa que a claim `role` do JWT **não representa o contexto real do usuário** quando ele opera em outra equipe.

**Problema 2 — Verificação de cargo sem contexto de equipe:**
```csharp
// Padrão repetido em TarefaService, MetaService, SugestaoService, etc.
var ehAdminOuDono = usuario.Equipes.Any(e => e.Cargo == Cargo.Dono || e.Cargo == Cargo.Admin);
```
Esta verificação concede privilégios de Admin/Dono sobre **qualquer recurso do sistema** se o usuário tiver esse cargo em **qualquer** equipe. Um Admin da Equipe B pode editar tarefas da Equipe A.

**Impacto:** Escalada de privilégios horizontal — cargos de uma equipe se aplicam incorretamente a recursos de outras equipes.

**Plano de refatoração para suportar multi-equipe/multi-cargo:**

**Etapa 1 — Simplificar o JWT (remover cargo como claim de autorização):**
```csharp
// TokenService: remover claims de cargo e equipeId do token
// Manter apenas: sub (userId), email, name, jti
// O cargo é contextual por equipe — não cabe em uma claim estática
```

**Etapa 2 — Criar método de verificação contextual nos Services:**
Para cada operação, o sistema precisa saber: *"qual é a equipe do recurso em questão?"* e *"qual é o cargo do solicitante nessa equipe específica?"*
```csharp
// Exemplo: ao verificar permissão sobre uma Tarefa
private async Task<bool> UsuarioEhAdminOuDonoDaTarefaAsync(Tarefa tarefa, int solicitanteId)
{
    // Busca os membros das equipes que possuem responsáveis na tarefa
    // Verifica se o solicitante é Admin/Dono em pelo menos uma dessas equipes
    var responsaveisIds = tarefa.ResponsaveisTarefas.Select(rt => rt.UsuarioId).ToList();
    var equipesDaTarefa = await _membroEquipeRepository
        .ObterEquipesEmComumAsync(responsaveisIds, solicitanteId);
    return equipesDaTarefa.Any(e => e.Cargo == Cargo.Dono || e.Cargo == Cargo.Admin);
}
```

**Etapa 3 — Para operações de equipe, usar sempre o `EquipeId` explícito:**
```csharp
// MembroEquipeService já faz isso corretamente — usar como referência:
var solicitanteMembro = await _membroEquipeRepository
    .ObterMembroPorEquipeEUsuarioAsync(dto.EquipeId, solicitanteId);
if (solicitanteMembro?.Cargo != Cargo.Dono) throw new ForbiddenException(...);
```

**Etapa 4 — Remover `[Authorize(Roles = "...")]` baseado na claim do token:**
```csharp
// UsuarioController.cs — este padrão deixa de funcionar corretamente
[Authorize(Roles = "Dono,Admin")] // ← baseia-se na claim do JWT (uma única equipe)
```
Substituir por verificação explícita nos Services com contexto de equipe.

---

### [LOG-03] 🟠 `AtualizarTarefaAsync` sobrescreve campos com valores padrão

**Arquivo:** [`TarefaService.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Services/Implementations/TarefaService.cs#L123-L139)

**Severidade:** 🟠 ALTO

**Problema:**
```csharp
var dataVencimentoUtc = dto.DataVencimento.Kind == DateTimeKind.Utc 
    ? dto.DataVencimento 
    : DateTime.SpecifyKind(dto.DataVencimento, DateTimeKind.Utc);
// ...
tarefa.Titulo = dto.Titulo;
tarefa.Descricao = dto.Descricao;
tarefa.DataVencimento = dataVencimentoUtc; // ← sobrescreve mesmo se não enviado
```
O `AtualizarTarefaDTO` provavelmente tem `DataVencimento` como não-nullable (`DateTime`). Se o cliente não enviar esse campo, ele chega como `DateTime.MinValue` e **sobrescreve** a data de vencimento original. O mesmo se aplica a `Titulo`, `Descricao`, `Prioridade`.

**Impacto:** Uma atualização parcial pode apagar dados críticos da tarefa.

**Correção:** Usar tipos nullable (`DateTime?`, `string?`) no DTO de atualização e aplicar patch somente nos campos não-nulos.

---

### [LOG-04] 🟠 `UsuarioService.DeletarUsuarioAsync` loga o próprio usuário como "DeletadoPor"

**Arquivo:** [`UsuarioService.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Services/Implementations/UsuarioService.cs#L136-L138)

**Severidade:** 🟠 MÉDIO

**Problema:**
```csharp
_logger.LogWarning("Auditoria: Exclusão de registro. Entidade: {Entidade}, Id: {Id}, DeletadoPor: {DeletadoPor}",
    "Usuario", id, id); // ← DeletadoPor = id (o próprio usuário deletado, não o solicitante)
```
O parâmetro `DeletadoPor` recebe `id` (o usuário sendo deletado) em vez do `solicitanteId` (quem executou a ação). A camada de serviço não recebe o `solicitanteId` — ele é checado na controller mas não passado para o service. Isso quebra a rastreabilidade de auditoria quando um Admin/Dono deleta outro usuário.

**Impacto:** Log de auditoria incorreto — impossível rastrear quem deletou o usuário.

**Correção:** Passar `solicitanteId` como parâmetro para `DeletarUsuarioAsync(int id, int solicitanteId)`.

---

### [LOG-05] 🟠 `SugestaoService.RejeitarSugestaoAsync` não persiste a rejeição

**Arquivo:** [`SugestaoService.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Services/Implementations/SugestaoService.cs#L89-L105)

**Severidade:** 🟠 ALTO

**Problema:**
```csharp
public async Task<ConsultaSugestaoDTO> RejeitarSugestaoAsync(int sugestaoId, int solicitanteId)
{
    var sugestao = await _sugestaoRepository.ObterSugestaoPorIdAsync(sugestaoId);
    // ...validações...
    _logger.LogInformation(...);
    return MapToConsultaDTO(sugestao); // ← retorna sem modificar nada no banco!
}
```
A rejeição de sugestão não altera nenhum campo na entidade `Sugestao`. Não há campo `Status` na entidade, então a sugestão rejeitada fica indistinguível de uma sugestão pendente. Pode ser aprovada ou rejeitada novamente indefinidamente.

**Impacto:** Violação da RN14 — o estado da sugestão nunca é persistido, permitindo que aprovações/rejeições sejam repetidas.

**Correção:** Adicionar campo `Status` à entidade `Sugestao` (`Pendente`, `Aprovada`, `Rejeitada`) e atualizar no banco ao aprovar/rejeitar.

---

### [LOG-06] 🟠 Verificação `ehAdminOuDono` não restrita à equipe do recurso — consolidado em LOG-02

> ⚠️ **Este issue está consolidado em [LOG-02]** acima, pois ambos derivam da mesma raiz: o modelo de autorização precisa ser contextual por equipe. Ver o plano de refatoração em LOG-02.

---

### [LOG-07] 🟡 Background Service dispara alertas de vencimento repetidamente

**Arquivo:** [`NotificacaoService.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Services/Implementations/NotificacaoService.cs#L164-L207)

**Severidade:** 🟡 MÉDIO

**Problema:**
```csharp
// Executado a cada hora
var tarefasPendentes = await _context.Tarefas
    .Where(t => t.Status == Status.Pendente && t.DataVencimento >= agoraUtc)
    .ToListAsync();
// Para cada tarefa, envia notificação se DataVencimento <= agoraUtc.AddHours(pref.AntecedenciaHoras)
```
Não há controle de "já enviado". A cada execução horária, se a tarefa ainda está dentro da janela de antecedência (ex: 24h), o alerta é disparado novamente. Um usuário com antecedência de 24h receberá **24 alertas** nas últimas 24h antes do vencimento.

**Impacto:** Spam de notificações para os usuários; viola RN12 indiretamente.

**Correção:** Registrar o último envio por `(usuarioId, tarefaId, canal)` para garantir que o alerta seja enviado no máximo uma vez por janela.

---

### [LOG-08] 🟡 `VerificarEmailAsync` no `AuthController` é endpoint público sem autenticação

**Arquivo:** [`AuthController.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Controllers/AuthController.cs#L130-L136)

**Severidade:** 🟡 MÉDIO

**Problema:**
```csharp
[HttpPost("verificar-email")]
[AllowAnonymous]
public async Task<IActionResult> VerificarEmail([FromBody] VerificarEmailDTO dto)
{
    var verificado = await _usuarioService.VerificarEmailAsync(dto.Email, dto.Codigo);
    return Ok(new { message = "E-mail verificado com sucesso!", email = dto.Email, verificado });
}
```
O endpoint retorna o e-mail do usuário na resposta mesmo em caso de sucesso, e não tem qualquer rate limiting. Combinado com o código fixo `"123456"`, qualquer pessoa pode verificar e-mails em massa.

**Impacto:** Quando SEC-02 for corrigido, sem rate limiting este endpoint ainda é vulnerável a ataques de força bruta no código.

**Correção:** Adicionar rate limiting; não retornar `email` na resposta de sucesso.

---

## 🔵 FALHAS ESTRUTURAIS E ARQUITETURAIS

---

### [ARQ-01] 🟠 `NotificacaoService` e `RelatorioService` violam a camada de repositório

**Arquivo:** [`NotificacaoService.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Services/Implementations/NotificacaoService.cs#L13) · [`RelatorioService.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Services/Implementations/RelatorioService.cs#L14)

**Severidade:** 🟠 ALTO

**Problema:**
```csharp
// Ambos os services injetam o DbContext diretamente
private readonly CortexDbContext _context;
```
`NotificacaoService` e `RelatorioService` injetam `CortexDbContext` diretamente, bypassando a camada de repositórios. A arquitetura definida no AGENTS.md estabelece explicitamente `Infra/Repositories` como camada de acesso a dados.

**Impacto:** Viola o padrão arquitetural; dificulta testes unitários (precisam de DbContext real/InMemory); acopla Services à implementação concreta de persistência.

**Correção:** Criar repositórios ou métodos específicos para as queries usadas; injetar via interface.

---

### [ARQ-02] 🟠 `SugestaoController` e `TicketReporteController` sem atributo `[Route]` de classe

**Arquivo:** [`SugestaoController.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Controllers/SugestaoController.cs#L11) · [`TicketReporteController.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Controllers/TicketReporteController.cs#L11) · [`AnexoController.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Controllers/AnexoController.cs#L11) · [`MetaController.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Controllers/MetaController.cs#L11)

**Severidade:** 🟠 ALTO

**Problema:**
```csharp
[ApiController]
[Authorize]
public class SugestaoController : ControllerBase // ← sem [Route]
```
Esses controllers **não possuem** o atributo `[Route]` no nível da classe. As rotas estão definidas diretamente em cada action com o caminho completo: `[HttpPost("api/tarefas/{id:int}/sugestoes")]`. Isso:
1. Foge do padrão `[Route("api/[controller]")]` dos outros controllers
2. Não segue a convenção do projeto
3. Qualquer refactor de namespace pode quebrar as rotas silenciosamente

**Impacto:** Inconsistência arquitetural; manutenção mais difícil; risco de conflito de rotas.

**Correção:** Adicionar `[Route]` na classe ou manter consistência com o padrão definido no projeto.

---

### [ARQ-03] 🟡 Método `ObterUsuarioIdAutenticado()` duplicado em todos os controllers

**Severidade:** 🟡 MÉDIO

**Problema:** O método privado `ObterUsuarioIdAutenticado()` é identicamente copiado em todos os controllers: `AuthController`, `EquipeController`, `TarefaController`, `UsuarioController`, `SugestaoController`, `TicketReporteController`, `AnexoController`, `MetaController`.

**Impacto:** Violação do princípio DRY; risco de divergência em future maintenance.

**Correção:** Extrair para um `BaseController` ou `ControllerExtensions`.

---

### [ARQ-04] 🟡 `TesteController` presente no projeto de produção

**Arquivo:** [`Controllers/TesteController.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Controllers/TesteController.cs)

**Severidade:** 🟡 MÉDIO

**Problema:** Existe um `TesteController.cs` no diretório de controllers de produção. Controllers de teste em ambientes de produção são vetores de ataque comuns.

**Correção:** Remover o arquivo ou garantir proteção adequada (`[Authorize]` + ambiente).

---

### [ARQ-05] 🟡 Token JWT com expiração padrão de 24 horas sem refresh token

**Arquivo:** [`TokenService.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Services/Implementations/TokenService.cs#L26) · [`appsettings.json`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/appsettings.json#L18)

**Severidade:** 🟡 MÉDIO

**Problema:**
```json
"ExpiresInMinutes": 1440 // 24 horas
```
Token de 24 horas sem mecanismo de refresh token ou revogação. Se um token for comprometido, ele permanece válido por até 24 horas sem nenhuma forma de invalidá-lo (não há blacklist de tokens).

**Correção:** Reduzir expiração (ex: 15-60 minutos) + implementar refresh token; ou implementar blacklist via Redis/banco.

---

### [ARQ-06] 🟡 `PasswordService` retorna mensagens de erro em inglês

**Arquivo:** [`PasswordService.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Services/Implementations/PasswordService.cs#L43-L68)

**Severidade:** 🟡 BAIXO

**Problema:**
```csharp
return (false, "Password is required");
return (false, "Password must be at least 8 characters long");
return (false, "Password must contain at least one uppercase letter");
```
Todo o sistema usa português mas `PasswordService` retorna erros em inglês que chegam até o cliente via `ArgumentException`.

**Correção:** Traduzir todas as mensagens para português.

---

### [ARQ-07] 🟡 `ListarAnexosPorEntidadeAsync` sem controle de acesso

**Arquivo:** [`AnexoService.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Services/Implementations/AnexoService.cs#L96-L99)

**Severidade:** 🟡 MÉDIO

**Problema:**
```csharp
public async Task<IEnumerable<ConsultaAnexoDTO>> ListarAnexosPorEntidadeAsync(TipoEntidade tipoEntidade, int entidadeId, int solicitanteId)
{
    var anexos = await _anexoRepository.ObterAnexosPorEntidadeAsync(tipoEntidade, entidadeId);
    return anexos.Select(MapToConsultaDTO); // ← sem verificação de permissão!
}
```
O parâmetro `solicitanteId` é recebido mas nunca utilizado. Qualquer usuário autenticado pode listar anexos de qualquer entidade apenas sabendo o `tipoEntidade` e `entidadeId`.

**Correção:** Implementar verificação de acesso à entidade antes de retornar os anexos.

---

### [ARQ-08] 🟡 Processamento de alertas de vencimento com N+1 queries

**Arquivo:** [`NotificacaoService.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Services/Implementations/NotificacaoService.cs#L177-L202)

**Severidade:** 🟡 MÉDIO

**Problema:**
```csharp
foreach (var tarefa in tarefasPendentes)
{
    foreach (var usuarioId in responsaveisIds)
    {
        var preferencias = await ObterPreferenciasAsync(usuarioId); // ← query por usuário por tarefa!
        // ...
    }
}
```
Para cada tarefa, para cada responsável, há uma query ao banco. Com 100 tarefas e 5 responsáveis cada, são 500+ queries por execução horária.

**Correção:** Pré-carregar todas as preferências dos responsáveis em uma única query antes dos loops.

---

### [ARQ-09] 🟡 `DateTime.SpecifyKind` em vez de converter corretamente

**Arquivo:** [`TarefaService.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Services/Implementations/TarefaService.cs#L43-L45) · múltiplos

**Severidade:** 🟡 BAIXO

**Problema:**
```csharp
var dataVencimentoUtc = dto.DataVencimento.Kind == DateTimeKind.Utc 
    ? dto.DataVencimento 
    : DateTime.SpecifyKind(dto.DataVencimento, DateTimeKind.Utc);
```
`DateTime.SpecifyKind` **não converte** o horário — apenas muda o `Kind` sem alterar o valor. Se o cliente enviar `"2026-10-30T15:00:00"` (sem timezone, interpretado como local), o sistema vai armazenar como se fosse UTC sem conversão, resultando em datas incorretas.

**Correção:** Usar `dto.DataVencimento.ToUniversalTime()` ou exigir que o cliente envie no formato ISO 8601 com offset (`DateTimeOffset`).

---

### [ARQ-10] 🟢 `PasswordService.ValidatePassword` não verifica a senha sendo igual à atual em `AlterarSenha`

**Arquivo:** [`UsuarioService.cs`](file:///c:/Users/yuma2/Repositorios/Cortex-backend/Services/Implementations/UsuarioService.cs#L152-L172)

**Severidade:** 🟢 BAIXO

**Problema:** Ao alterar senha, o sistema não verifica se a nova senha é igual à senha atual. Um usuário pode "alterar" a senha para o mesmo valor sem notificação.

**Correção:** Adicionar validação `if (_passwordService.VerifyPassword(novaSenha, usuario.SenhaHash)) throw new ArgumentException("A nova senha não pode ser igual à senha atual.")`.

---

## 📋 Plano de Correção — Prioridade

### 🔴 Imediato (antes de qualquer deploy)

| # | Issue | Ação |
|---|-------|------|
| 1 | **SEC-01** | *(Aceitável em dev)* Preparar `appsettings.Production.json` fora do Git; usar variáveis de ambiente em produção |
| 2 | **SEC-02 + SEC-03** | Implementar geração e armazenamento seguro de código de verificação de e-mail |
| 3 | **SEC-04** | Condicionar `RequireHttpsMetadata` ao ambiente |
| 4 | **SEC-05** | Reordenar `UseRateLimiter()` para antes de `UseAuthentication()` |
| 5 | **LOG-01** | Corrigir lógica da RN08 para verificar todas as tarefas antes de concluir meta |
| 6 | **LOG-05** | Adicionar campo `Status` à `Sugestao` e persistir aprovação/rejeição |

### 🟠 Curto prazo (próxima sprint)

| # | Issue | Ação |
|---|-------|------|
| 7 | **SEC-06** | Unificar mensagem de erro no login |
| 8 | **SEC-07** | Adicionar validação de MIME type/extensão em todos os endpoints de upload |
| 9 | **LOG-02** | **Refatorar modelo de autorização multi-equipe** (ver detalhes abaixo) |
| 10 | **LOG-03** | Converter `AtualizarTarefaDTO` para usar tipos nullable |
| 11 | **LOG-04** | Passar `solicitanteId` para `DeletarUsuarioAsync` |
| 12 | **ARQ-01** | Eliminar uso direto de `CortexDbContext` em Services |
| 13 | **ARQ-02** | Padronizar rotas nos controllers sem `[Route]` |

#### 🔧 Detalhamento da refatoração LOG-02 — Autorização Multi-Equipe

Esta é a maior refatoração do bloco de curto prazo e deve ser feita em sequência:

**Passo 1 — Repositório:** Adicionar método `ObterMembrosPorUsuarioEEquipesAsync(int usuarioId, IEnumerable<int> equipeIds)` ao `IMembroEquipeRepository` para buscar os cargos do usuário em um conjunto de equipes de uma só vez (evitar N+1).

**Passo 2 — TokenService:** Remover as claims `cargo` e `equipeId` do JWT, ou mantê-las apenas como metadado informativo. A autorização nunca deve depender delas para decisões de permissão.

**Passo 3 — Services:** Substituir o padrão `usuario.Equipes.Any(e => e.Cargo == Cargo.Admin)` por verificações contextuais:
- **Operações em Tarefa** → verificar cargo na(s) equipe(s) cujos membros são responsáveis pela tarefa
- **Operações em Equipe** → verificar cargo usando o `EquipeId` explícito do endpoint (padrão já correto em `MembroEquipeService`)
- **Operações em Meta/Sugestão/Ticket** → verificar cargo na equipe da tarefa vinculada

**Passo 4 — Controllers:** Remover `[Authorize(Roles = "Dono,Admin")]` do `UsuarioController.Listar()` e substituir por verificação de serviço; ou aceitar que qualquer usuário autenticado pode listar usuários (dependendo da decisão de produto).

### 🟡 Médio prazo (refinamento)

| # | Issue | Ação |
|---|-------|------|
| 15 | **LOG-07** | Implementar controle de "alerta já enviado" no `NotificacaoService` |
| 16 | **LOG-08** | Adicionar rate limiting no endpoint de verificação de e-mail |
| 17 | **ARQ-03** | Criar `BaseController` com `ObterUsuarioIdAutenticado()` |
| 18 | **ARQ-04** | Remover `TesteController` do projeto |
| 19 | **ARQ-05** | Reduzir expiração do JWT e implementar refresh token |
| 20 | **ARQ-07** | Implementar controle de acesso em `ListarAnexosPorEntidadeAsync` |
| 21 | **ARQ-08** | Otimizar queries N+1 no processamento de alertas |
| 22 | **ARQ-09** | Substituir `DateTime.SpecifyKind` por `DateTimeOffset` ou `.ToUniversalTime()` |
| 23 | **SEC-08** | Logar erros de deleção de arquivo |
| 24 | **ARQ-06** | Traduzir mensagens do `PasswordService` para português |
| 25 | **ARQ-10** | Validar nova senha diferente da atual no `AlterarSenha` |
