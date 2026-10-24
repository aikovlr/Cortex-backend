# AGENTS.md — Cortex Backend

Guia e especificação de engenharia do backend para o **Cortex**, derivado do documento oficial de especificação e modelagem do sistema (*Charter*). Este documento contém exclusivamente as informações, regras de negócio e restrições relevantes para a arquitetura, regras de domínio e persistência do backend.

---

## 1. Visão Geral do Sistema

O **Cortex** é um sistema transacional de gerenciamento de tarefas administrativas, pessoais e colaborativas voltado para ambientes corporativos ágeis. 

O backend é responsável por:
- Controle de identidade, autenticação e autorização baseada em cargos (RBAC).
- Gestão do ciclo de vida de tarefas, delegações e workflow colaborativo.
- Fluxo de sugestões de alteração em tarefas (com aprovação/rejeição do criador).
- Gamificação e acompanhamento de metas vinculadas a tarefas.
- Canal de reporte e tickets sobre tarefas atribuídas diretamente aos superiores.
- Agendamento de alertas e notificações assíncronas (e-mail, push notifications e SMS).
- Auditoria de ações críticas e geração de relatórios com restrição de visibilidade.

---

## 2. Stack Tecnológica & Arquitetura

- **Linguagem / Runtime**: C# / .NET 10 (ASP.NET Core Web API)
- **Persistência / ORM**: Entity Framework Core 10 com `Npgsql.EntityFrameworkCore.PostgreSQL`
- **Banco de Dados**: PostgreSQL (com tipos Enum nativos mapeados via Npgsql)
- **Criptografia & Segurança**: `BCrypt.Net-Next` para hash de senhas, JWT / Claims para autorização
- **Validação**: `FluentValidation.AspNetCore` para validação de contratos e DTOs
- **Auditoria & Logs**: `Serilog.AspNetCore` para logs estruturados de auditoria
- **Documentação de API**: `Microsoft.AspNetCore.OpenApi` (OpenAPI v3)

### Estrutura de Camadas do Projeto
```
Cortex-backend/
├── Controllers/       # Endpoints HTTP (REST), roteamento e responses
├── DTOs/              # Data Transfer Objects (Requests, Responses, Filters)
├── Entities/          # Entidades de Domínio e Enums
│   └── ENUMS/         # Status, Prioridade, Cargo, TipoEntidade
├── Infra/             # Persistência e Integrações
│   ├── Configuration/ # Mapeamentos Fluent API (IEntityTypeConfiguration)
│   ├── Persistence/   # CortexDbContext e Migrations
│   └── Repositories/  # Repositórios de dados
└── Services/          # Regras de Negócio e Casos de Uso
    ├── Interfaces/    # Contratos de serviços
    └── Implementations/ # Implementações das regras de negócio
```

---

## 3. Modelo de Domínio e Banco de Dados

### 3.1. Enums do Sistema

| Enum | Valores Permitidos | Descrição |
| :--- | :--- | :--- |
| `Status` | `Pendente`, `Atrasada` (ou `Em atraso`), `Concluida` | Ciclo de vida da tarefa |
| `Prioridade` | `Baixa`, `Media`, `Alta`, `Urgente` | Nível de urgência da tarefa |
| `Cargo` | `Dono`, `Admin`, `Membro` | Papel do usuário dentro de uma equipe |
| `TipoEntidade` | `Tarefa`, `ResponsavelTarefa`, `Usuario` | Entidade à qual um anexo pertence |

### 3.2. Entidades & Tabelas Principais

- **`Usuario` (`Usuarios`)**:
  - `Id` (`int`, PK)
  - `Nome` (`string`, max 255, obrigatório)
  - `Email` (`string`, max 255, obrigatório, **UNIQUE**)
  - `SenhaHash` (`string`, max 255, obrigatório, hash via BCrypt)
  - `Telefone` (`string`, max 20, opcional)
  - `DataCriacao` (`DateTime`, UTC, obrigatório)
  - Relacionamentos: TarefasCriadas, Respostas, ResponsaveisTarefas, Equipes, Tickets, Sugestoes, Metas, FotoPerfil (Anexo).

- **`Equipe` (`Equipes`)**:
  - `Id` (`int`, PK)
  - `Nome` (`string`, max 255, obrigatório)
  - `DataCriacao` (`DateTime`, UTC, obrigatório)
  - Relacionamentos: Membros (`MembroEquipe`).

- **`MembroEquipe` (`MembrosEquipe`)**:
  - `Id` (`int`, PK)
  - `DataEntrada` (`DateTime`, UTC, obrigatório)
  - `Cargo` (`Enum Cargo`: `Dono`, `Admin`, `Membro`, obrigatório)
  - `UsuarioId` (`int`, FK -> `Usuario`)
  - `EquipeId` (`int`, FK -> `Equipe`)

- **`Tarefa` (`Tarefas`)**:
  - `Id` (`int`, PK)
  - `Titulo` (`string`, max 255, obrigatório)
  - `Descricao` (`string`, opcional)
  - `DataVencimento` (`DateTime`, obrigatório)
  - `Pontuacao` (`int`, opcional)
  - `Status` (`Enum Status`: `Pendente`, `Atrasada`, `Concluida`, obrigatório)
  - `Prioridade` (`Enum Prioridade`: `Baixa`, `Media`, `Alta`, `Urgente`, obrigatório)
  - `CriadorId` (`int`, FK -> `Usuario`)
  - Relacionamentos: ResponsaveisTarefas, Respostas, Anexos, Tickets, Sugestoes, Metas.

- **`ResponsavelTarefa` (`ResponsaveisTarefa`)**:
  - `Id` (`int`, PK)
  - `TarefaId` (`int`, FK -> `Tarefa`)
  - `UsuarioId` (`int`, FK -> `Usuario`, opcional se atribuído a fila/equipe)

- **`RespostaTarefa` (`RespostasTarefa`)**:
  - `Id` (`int`, PK)
  - `DataResposta` (`DateTime`, UTC, obrigatório)
  - `UsuarioId` (`int`, FK -> `Usuario`)
  - `TarefaId` (`int`, FK -> `Tarefa`)

- **`Sugestao` (`Sugestoes`)**:
  - `Id` (`int`, PK)
  - `Descricao` (`string`, obrigatório)
  - `DataCriacao` (`DateTime`, UTC, obrigatório)
  - `TarefaId` (`int`, FK -> `Tarefa`)
  - `UsuarioId` (`int`, FK -> `Usuario`)

- **`TicketReporte` (`TicketReporte`)**:
  - `Id` (`int`, PK)
  - `Descricao` (`string`, obrigatório)
  - `Ativo` (`bool`, obrigatório, default `true`)
  - `DataCriacao` (`DateTime`, UTC, obrigatório)
  - `TarefaId` (`int`, FK -> `Tarefa`)
  - `UsuarioId` (`int`, FK -> `Usuario`)

- **`Meta` (`Metas`)**:
  - `Id` (`int`, PK)
  - `Nome` (`string`, max 255, obrigatório)
  - `DataFechamento` (`DateTime`, opcional)
  - `Concluida` (`bool`, opcional, default `false`)
  - `TarefaId` (`int`, FK -> `Tarefa`)
  - `UsuarioId` (`int`, FK -> `Usuario`)

- **`Anexo` (`Anexos`)**:
  - `Id` (`int`, PK)
  - `UrlCaminho` (`string`, max 255, obrigatório)
  - `NomeOriginal` (`string`, max 255, obrigatório)
  - `MimeType` (`string`, max 255, obrigatório)
  - `DataEnvio` (`DateTime`, UTC, obrigatório)
  - `TipoEntidade` (`Enum TipoEntidade`: `Tarefa`, `ResponsavelTarefa`, `Usuario`)
  - FKs opcionais de vínculo (`TarefaId`, `RespostaTarefaId`, `UsuarioId`).

---

## 4. Requisitos Funcionais do Backend (RF)

| Código | Requisito | Escopo / Responsabilidade no Backend |
| :--- | :--- | :--- |
| **RF001** | **Gerenciar Usuários** | CRUD completo de usuários. Validação de formato de e-mail e hash da senha com BCrypt antes de persistir. Vínculo de foto de perfil via Anexo. |
| **RF002** | **Gerenciar Categorias** | CRUD de categorias e associação N:N ou 1:N com tarefas. |
| **RF003** | **Gerenciar Tarefas** | CRUD de tarefas com controle de datas, prioridade, pontuação e status. Atribuição individual (`ResponsavelTarefa`) ou em lote por equipe. |
| **RF004** | **Sugestões de Alteração** | Permite que o responsável pela tarefa submeta uma sugestão de alteração; criador recebe notificação e pode aprovar ou recusar a proposta. |
| **RF005** | **Preferências de Conta e Alertas** | Configuração de canal (e-mail, push, SMS) e frequência de notificação para nova atribuição, tarefa próxima do vencimento, conclusão de meta e tickets de reporte. |
| **RF006** | **Geração de Relatórios** | Emissão de relatórios e agregação de dados com filtros (status, categoria, prioridade, período, equipe, responsável). Restrição estrita de visualização conforme perfil. |
| **RF007** | **Gerenciamento de Metas** | CRUD de metas associadas a tarefas e usuários. Cálculo automático de conclusão baseado nas tarefas vinculadas. |
| **RF008** | **Reportar Tarefas (Tickets)** | Criação de ticket de reporte por parte do executor da tarefa quando há impedimento/problema, roteando para os donos/administradores superiores. |
| **RF009** | **Gerenciar Equipes** | CRUD de equipes e gerenciamento de membros associados com definição de cargos (`Dono`, `Admin`, `Membro`). |

---

## 5. Regras de Negócio Críticas (RN)

Toda regra abaixo deve ser estritamente garantida no backend via validações (Services, FluentValidation e Constraints de Banco):

1. **RN01 — E-mail Único**: Um endereço de e-mail só pode estar vinculado a uma única conta no sistema (`Unique Index` no PostgreSQL).
2. **RN02 — Complexidade de Senha**: A senha deve possuir no mínimo 8 caracteres, contendo letras maiúsculas, minúsculas, números e caracteres especiais.
3. **RN03 — Gestão de Cargos na Equipe**: Somente usuários com o cargo `Dono` podem alterar o cargo de outro usuário dentro da respectiva equipe.
4. **RN04 — Adição e Remoção de Membros**: Somente `Dono` e `Admin` da equipe possuem permissão para adicionar ou remover membros.
5. **RN05 — Impedimento de Exclusão de Conta**: Um usuário **não pode** excluir sua própria conta se possuir tarefas atribuídas a ele com status `Pendente` ou em andamento.
6. **RN06 — Tarefa Atrasada & Notificação**: Tarefas cuja `DataVencimento < DateTime.UtcNow` e `Status != Concluida` devem transicionar automaticamente para o status `Atrasada` e emitir notificação imediata ao responsável.
7. **RN07 — Validação de Metas**: Toda meta deve estar vinculada a no mínimo uma tarefa válida para ser considerada elegível.
8. **RN08 — Conclusão de Meta**: O sistema só deve marcar uma meta como `Concluida = true` se todas as tarefas relacionadas a ela estiverem com status `Concluida`.
9. **RN09 — Limite de Relatórios para Usuário Comum**: Usuários comuns (`Membro`) só podem gerar relatórios e métricas de suas próprias tarefas.
10. **RN10 — Escopo de Relatórios Administrativos**: `Dono` e `Admin` podem extrair relatórios gerais e consolidados filtrados por equipe, período, prioridade, categoria e status.
11. **RN11 — Janela de Antecedência de Alertas**: A antecedência para disparo de notificações de tarefas a vencer deve ser configurável entre o mínimo de **1 hora** e o máximo de **7 dias**.
12. **RN12 — Descarte de Notificações Expiradas**: Alertas cujo prazo de disparo já passou antes do envio não devem ser enviados retroativamente de forma automática.
13. **RN13 — Confirmação de E-mail para Alertas**: O envio de notificações por e-mail só pode ser ativado caso o e-mail do usuário esteja verificado/confirmado.
14. **RN14 — Aprovação de Sugestões**: Sugestões de alteração em uma tarefa exigem aprovação explícita do criador da tarefa antes de suas alterações surtirem efeito no registro principal.
15. **RN15 — Validade Máxima de Metas**: Metas criadas devem ter uma data de término/validade de no máximo **12 meses** a partir da data de criação.

---

## 6. Requisitos Não Funcionais (RNF)

- **RNF01 (Segurança & Autenticação)**: Criptografia de senhas com salting (BCrypt), autenticação baseada em tokens (JWT) e autorização com RBAC / Claims.
- **RNF04 (Concorrência)**: Suporte a múltiplos acessos simultâneos sem corrupção de dados (uso adequado de transações e queries assíncronas `async/await`).
- **RNF05 (Auditoria & Rastreabilidade)**: Gravação de logs estruturados com `Serilog` para eventos críticos (login, alteração/conclusão de tarefas, exclusão de registros e tickets).
- **RNF07 (Persistência)**: Persistência integral em PostgreSQL utilizando mapeamentos do EF Core com migrations controladas.
- **RNF08 (Desenvolvimento Seguro)**: Sanitização e validação de entrada em todos os endpoints, prevenção a SQL Injection via Entity Framework e resposta uniforme a erros (RFC 7807 - `ProblemDetails`).
- **RNF09 (Performance de Ações Críticas)**: O tempo de resposta para operações transacionais críticas (criação, edição e exclusão de tarefas) deve ser inferior a **2 segundos**.
- **RNF10 (Serviço de Notificações)**: Envio assíncrono para os gateways de entrega (provedores Push FCM/APNs, SMTP de e-mail e Gateway SMS) sem bloquear a requisição HTTP do usuário.

---

## 7. Instruções e Convenções para Desenvolvimento

1. **DTOs e Isolamento de Domínio**:
   - Nunca expor entidades de banco (`Entities`) diretamente nos retornos das Controllers.
   - Sempre definir DTOs específicos de Request e Response sob a pasta `DTOs/`.
2. **Validações**:
   - Manter regras de formato e presença nos validadores do FluentValidation.
   - Manter regras de integridade e fluxo de negócio na camada `Services`.
3. **Tratamento de Exceções**:
   - Evitar retornar `500 Internal Server Error` descontrolado; capturar exceções de domínio e retornar status HTTP semânticos (`400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`, `409 Conflict`).
4. **Utc DateTimes**:
   - Todas as datas manipuladas pelo backend devem estar normalizadas em UTC (`DateTime.UtcNow`).
5. **Enums do PostgreSQL**:
   - Qualquer inclusão ou alteração de enum deve ser refletida tanto em `Entities/ENUMS` quanto no registro de enums do `Program.cs` e no `CortexDbContext.OnModelCreating`.
