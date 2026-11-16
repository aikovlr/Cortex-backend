# 🧠 Cortex Backend — API & Arquitetura

API RESTful corporativa de alta performance do ecossistema **Cortex**, desenvolvida em **.NET 10 (C#)** e **Entity Framework Core**, com persistência em **PostgreSQL**. O sistema gerencia autenticação, controle de tarefas transacionais, governança de equipes, acompanhamento de metas com gamificação, notificações multicanal assíncronas e relatórios analíticos de produtividade.

---

## 📑 Sumário

1. [Visão Geral & Tecnologias](#-visão-geral--tecnologias)
2. [Arquitetura & Estrutura](#-arquitetura--estrutura)
3. [Segurança & Proteção de Dados](#-segurança--proteção-de-dados)
4. [Documentação Simplificada dos Endpoints](#-documentação-simplificada-dos-endpoints)
5. [Serviços em Segundo Plano (Background Services)](#-serviços-em-segundo-plano-background-services)
6. [Suíte de Testes Automatizados](#-suíte-de-testes-automatizados)
7. [Configuração & Execução Local](#-configuração--execução-local)
8. [Procedimentos de Backup (RNF006)](#-procedimentos-de-backup-rnf006)

---

## 🚀 Visão Geral & Tecnologias

- **Framework**: .NET 10.0 (ASP.NET Core Web API)
- **Linguagem**: C# 13
- **ORM / Banco de Dados**: Entity Framework Core 10 com Npgsql (PostgreSQL)
- **Autenticação**: JWT (JSON Web Token) com armazenamento prioritário em Cookie `HttpOnly; Secure; SameSite=None` e suporte a Header `Authorization: Bearer`
- **Validação**: FluentValidation integrado ao pipeline de injeção de dependência
- **Logs Estruturados**: Serilog com rotação diária de arquivos JSON
- **Documentação Interativa**: OpenAPI / Swagger (`/openapi/v1.json`)
- **Tratamento de Exceções**: Padronização RFC 9457 / RFC 7807 (`ProblemDetails`)

---

## 🏗 Arquitetura & Estrutura

O projeto adota uma arquitetura em camadas orientada a serviços e domínio (*Service-Repository Pattern*):

```
Cortex-backend/
├── Controllers/            # Controladores REST com roteamento e controle de acesso
├── DTOs/                   # Data Transfer Objects (Requests e Responses tipados)
│   ├── Request/            # DTOs de criação, atualização e filtros
│   └── Response/           # DTOs de retorno e consulta de dados
├── Entities/               # Modelos de domínio do banco (Code-First)
├── Infra/                  # Infraestrutura técnica da aplicação
│   ├── Configuration/      # Mapeamento do EF Core (Fluent API) e opções tipadas
│   ├── Gateways/           # Gateways de E-mail (SMTP/Log), Push e SMS
│   ├── Middleware/         # ExceptionHandlingMiddleware (RFC 9457)
│   └── Persistence/        # CortexDbContext e Migrations do banco
├── Repositories/           # Interfaces e implementações de acesso a dados
├── Services/               # Regras de negócio da aplicação
│   ├── Background/         # HostedServices para tarefas em segundo plano
│   ├── Implementations/    # Lógica de negócio e orquestração de serviços
│   └── Interfaces/         # Contratos e abstrações dos serviços
├── Validators/             # Regras de validação estrita com FluentValidation
└── Cortex.Tests/           # Suíte de testes unitários e de integração (194 testes)
```

---

## 🔒 Segurança & Proteção de Dados

A segurança foi implementada como um pilar central em todas as camadas da aplicação:

### 1. Autenticação Segura via Cookie HttpOnly + JWT
* **Mitigação de XSS**: Ao realizar o login (`POST /api/auth/login`), a API grava automaticamente o token JWT em um cookie de sessão com as diretivas `HttpOnly; Secure; SameSite=None; Path=/; Expires=60min`. Isso impede que scripts maliciosos injetados no navegador (XSS) tenham acesso ao token.
* **Compatibilidade**: Chamadas via cliente HTTP ou Postman continuam recebendo o token no corpo da resposta para uso opcional via header `Authorization: Bearer <token>`.

### 2. Invalidação Imediata de Sessões Anteriores (`pwd_at`)
* Cada token emitido contém o *claim* `pwd_at` (timestamp da última alteração de senha).
* No evento `OnTokenValidated` do middleware de autenticação, o timestamp do token é comparado com o campo `SenhaAlteradaEm` do usuário no banco. Caso a senha tenha sido alterada após a emissão do token, a sessão é **invalidada instantaneamente**, forçando o encerramento em todos os navegadores e dispositivos.

### 3. Criptografia e Armazenamento de Senhas
* Senhas de usuário nunca são armazenadas em texto claro.
* É utilizado **BCrypt** com fator de trabalho seguro (cost factor) e *salt* criptográfico individual gerado automaticamente.
* O `PasswordService` impõe requisitos estritos de complexidade (mínimo de 8 caracteres, letras maiúsculas, minúsculas, dígitos numéricos e símbolos especiais).

### 4. Rate Limiting Nativo (Proteção Contra Força Bruta)
Configurado diretamente no pipeline do ASP.NET Core com janelas fixas:
* `login-policy`: Máximo de 5 tentativas por minuto para `/api/auth/login`.
* `verificacao-email-policy`: Máximo de 3 solicitações por minuto para `/api/auth/solicitar-verificacao-email`.
* `esqueci-senha-policy`: Máximo de 3 solicitações por 5 minutos para solicitação de redefinição de senha.
* `redefinir-senha-policy`: Máximo de 5 tentativas por 5 minutos para submissão de nova senha.
* Requisições excedentes recebem automaticamente status **HTTP 429 Too Many Requests**.

### 5. Padronização RFC 9457 (`ProblemDetails`)
* O middleware customizado `ExceptionHandlingMiddleware` captura qualquer falha interna e padroniza a resposta em conformidade com a RFC 9457 / RFC 7807.
* **Segurança por Omissão**: Detalhes internos de infraestrutura, chamadas de sistema e *stack traces* nunca são vazados para o cliente em ambiente de produção ou desenvolvimento.

### 6. Gestão Segura de Credenciais (Sem Segredos Versionados)
* O arquivo `appsettings.json` versionado no Git contém apenas chaves vazias e estruturas de exemplo.
* Em ambiente de desenvolvimento local, credenciais reais (como string de conexão e chave de assinatura JWT) são gerenciadas exclusivamente através do **.NET User Secrets** (`c5bfbcd1-827e-47f8-8df5-0b0006bc5501`).
* Em produção, a injeção é realizada via variáveis de ambiente (`ASPNETCORE_ConnectionStrings__DefaultConnection` e `ASPNETCORE_Jwt__Secret`).

### 7. Validação e Sanitização de Anexos
* O `ArquivoValidacaoService` aplica verificação de extensão permitida (imagens JPEG, PNG, WebP e documentos PDF, DOC, DOCX, XLS, XLSX) e limite de tamanho (máximo de 10 MB por arquivo).

---

## 📋 Documentação Simplificada dos Endpoints

> Todos os endpoints que exigem autenticação necessitam do cookie de sessão `cortex_token` ou do header `Authorization: Bearer <token>`.

### 1. Autenticação & Acesso (`/api/auth`)
| Método | Endpoint | Acesso | Descrição |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/auth/login` | Público *(Rate Limit)* | Autentica o usuário, retorna perfil e grava cookie `cortex_token` |
| `POST` | `/api/auth/logout` | Autenticado | Revoga/expira o cookie de autenticação `cortex_token` |
| `POST` | `/api/auth/alterar-senha` | Autenticado | Altera a senha do usuário autenticado e invalida sessões antigas |
| `POST` | `/api/auth/solicitar-verificacao-email` | Público *(Rate Limit)* | Gera e envia código de 6 dígitos para o e-mail cadastrado |
| `POST` | `/api/auth/verificar-email` | Público | Valida o código recebido e confirma a conta de e-mail |
| `POST` | `/api/auth/esqueci-senha` | Público *(Rate Limit)* | Envia código de recuperação de senha para a caixa postal |
| `POST` | `/api/auth/redefinir-senha` | Público *(Rate Limit)* | Redefine a senha utilizando o código de segurança verificado |

### 2. Usuários & Perfil (`/api/usuario`)
| Método | Endpoint | Acesso | Descrição |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/usuario` | Público | Cadastro de novo usuário na plataforma com termos LGPD |
| `GET` | `/api/usuario/me` | Autenticado | Retorna os dados cadastrais, cargo, nível e foto do usuário logado |
| `PUT` | `/api/usuario/me` | Autenticado | Atualiza nome e telefone do usuário autenticado |
| `POST` | `/api/usuario/foto-perfil` | Autenticado | Upload de imagem de perfil (`multipart/form-data`) |
| `DELETE` | `/api/usuario/foto-perfil` | Autenticado | Remove a foto de perfil atual do usuário |
| `DELETE` | `/api/usuario/minha-conta` | Autenticado | Exclui a conta do usuário (bloqueado se houver tarefas pendentes - RN05) |
| `GET` | `/api/usuario/{id}` | Autenticado | Consulta dados públicos de um usuário por ID |

### 3. Tarefas (`/api/tarefa` e `/api/tarefas`)
| Método | Endpoint | Acesso | Descrição |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/tarefa` | Autenticado | Lista tarefas com filtros por status, prioridade, responsável ou criador |
| `POST` | `/api/tarefa` | Autenticado | Cria uma nova tarefa com anexos opcionais (`multipart/form-data`) |
| `GET` | `/api/tarefa/{id}` | Autenticado | Detalha uma tarefa, responsáveis, anexos e respostas |
| `PUT` | `/api/tarefa/{id}` | Criador / Dono | Atualiza título, descrição, prazo, prioridade e pontuação |
| `DELETE` | `/api/tarefa/{id}` | Criador / Dono | Exclui uma tarefa e seus relacionamentos vinculados |
| `PATCH` | `/api/tarefa/{id}/concluir` | Responsável | Conclui a tarefa pelo responsável |
| `PATCH` | `/api/tarefas/{id}/cancelar-conclusao` | Responsável | Reverte a conclusão da tarefa para o estado pendente |
| `POST` | `/api/tarefa/{id}/responsaveis` | Criador / Dono | Adiciona responsáveis adicionais à tarefa |
| `DELETE` | `/api/tarefa/{id}/responsaveis/{usuarioId}` | Criador / Dono | Remove um responsável da tarefa |

### 4. Equipes & Membros (`/api/equipe`)
| Método | Endpoint | Acesso | Descrição |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/equipe` | Autenticado | Lista equipes das quais o usuário autenticado participa |
| `POST` | `/api/equipe` | Autenticado | Cria uma nova equipe (o criador torna-se `Dono`) |
| `GET` | `/api/equipe/{id}` | Membro | Obtém detalhes da equipe |
| `PUT` | `/api/equipe/{id}` | Dono / Admin | Atualiza o nome ou descrição da equipe |
| `DELETE` | `/api/equipe/{id}` | Dono | Exclui permanentemente a equipe |
| `GET` | `/api/equipe/{id}/membros` | Membro | Lista todos os membros, e-mails, níveis de XP e cargos |
| `POST` | `/api/equipe/{id}/membros` | Dono / Admin | Adiciona um colaborador à equipe via e-mail |
| `DELETE` | `/api/equipe/{id}/membros/{membroId}` | Dono / Admin | Remove um membro da equipe (respeita hierarquia) |
| `PATCH` | `/api/equipe/{id}/membros/{membroId}/cargo`| Dono | Altera o cargo de um participante (`Admin` ou `Membro`) |

### 5. Metas & Gamificação (`/api/metas`)
| Método | Endpoint | Acesso | Descrição |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/metas` | Autenticado | Lista metas do usuário ou de equipes com percentual de progresso |
| `POST` | `/api/metas` | Autenticado | Cria uma meta associada a tarefas obrigatórias e prazo (até 12 meses) |
| `GET` | `/api/metas/{id}` | Autenticado | Obtém detalhes, tarefas vinculadas e progresso de uma meta |
| `PUT` | `/api/metas/{id}` | Criador / Dono | Atualiza dados cadastrais e tarefas vinculadas da meta |
| `DELETE` | `/api/metas/{id}` | Criador / Dono | Exclui uma meta existente |
| `GET` | `/api/tarefas/{id}/metas` | Autenticado | Lista metas vinculadas a uma tarefa específica |

### 6. Sugestões de Alteração (`/api/sugestoes`)
| Método | Endpoint | Acesso | Descrição |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/tarefas/{id}/sugestoes` | Responsável | Envia proposta de alteração de prazo ou descrição ao criador |
| `GET` | `/api/tarefas/{id}/sugestoes` | Autenticado | Lista sugestões enviadas para a tarefa |
| `PATCH` | `/api/sugestoes/{id}/aprovar` | Criador da Tarefa | Aprova a sugestão e atualiza automaticamente os dados da tarefa |
| `PATCH` | `/api/sugestoes/{id}/rejeitar` | Criador da Tarefa | Rejeita a proposta com justificativa opcional |

### 7. Tickets de Reporte & Dúvidas (`/api/tickets`)
| Método | Endpoint | Acesso | Descrição |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/tarefas/{id}/tickets` | Responsável | Abre um ticket de impedimento técnico vinculado a uma tarefa |
| `GET` | `/api/tarefas/{id}/tickets` | Autenticado | Lista tickets abertos de uma tarefa |
| `GET` | `/api/equipes/{id}/tickets` | Membro da Equipe | Lista todos os tickets de suporte abertos nas tarefas da equipe |
| `PATCH` | `/api/tickets/{id}/fechar` | Criador / Dono | Encerra e conclui o ticket de suporte |

### 8. Respostas de Tarefas & Avaliação (`/api/respostas`)
| Método | Endpoint | Acesso | Descrição |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/tarefas/{id}/respostas` | Responsável | Envia resposta/entrega de tarefa com anexos (`multipart/form-data`) |
| `GET` | `/api/tarefas/{id}/respostas` | Autenticado | Lista respostas submetidas para a tarefa |
| `PATCH` | `/api/respostas/{id}/avaliar` | Criador da Tarefa | Atribui nota quantitativa à entrega respeitando a pontuação máxima |

### 9. Notificações & Preferências (`/api/notificacoes`)
| Método | Endpoint | Acesso | Descrição |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/notificacoes` | Autenticado | Central de notificações no banco com suporte a filtros e paginação |
| `PATCH` | `/api/notificacoes/{id}/lida` | Autenticado | Marca notificação individual como visualizada |
| `PATCH` | `/api/notificacoes/marcar-todas-lidas` | Autenticado | Marca todo o histórico de notificações do usuário como lido |
| `GET` | `/api/notificacoes/preferencias` | Autenticado | Retorna configuração de canais e antecedência de alerta |
| `PUT` | `/api/notificacoes/preferencias` | Autenticado | Atualiza canais ativos (`Email`, `Push`, `SMS`) e horas de antecedência |

### 10. Acessibilidade (`/api/preferencias/acessibilidade`)
| Método | Endpoint | Acesso | Descrição |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/preferencias/acessibilidade` | Autenticado | Carrega preferências visuais sincronizadas do usuário |
| `PUT` | `/api/preferencias/acessibilidade` | Autenticado | Sincroniza tema (`light`, `dark`), alto contraste e tamanho de fonte |

### 11. Relatórios Analíticos (`/api/relatorios`)
| Método | Endpoint | Acesso | Descrição |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/relatorios/tarefas` | Autenticado | Retorna KPIs, percentual de conclusão e métricas por período e status |
| `GET` | `/api/relatorios/equipes/{id}` | Dono / Admin | Relatório consolidado com matriz de desempenho individual por membro |

---

## ⚙️ Serviços em Segundo Plano (Background Services)

A API executa 3 background workers nativos que garantem a integridade operacional e o atendimento às regras de negócio de forma assíncrona:

1. **`TarefaStatusBackgroundService` (RN06)**:
   - Monitora tarefas periodicamente.
   - Atualiza automaticamente tarefas não concluídas que ultrapassaram a `DataVencimento` para o status `Atrasada`.
2. **`AlertaVencimentoBackgroundService` (RN11)**:
   - Cruza a data atual com as preferências de antecedência de cada usuário.
   - Envia lembretes preventivos de proximidade de prazo via canais configurados (E-mail, Push ou SMS), evitando alertas duplicados com persistência de controle de envios.
3. **`NotificacaoDispatcherBackgroundService` (RNF10)**:
   - Despacha notificações enfileiradas na fila em memória assíncrona (`INotificacaoQueue`), liberando as requisições HTTP imediatamente sem bloqueio por latência de envio de e-mails/gateways.

---

## 🧪 Suíte de Testes Automatizados

O repositório possui uma suíte rigorosa de **194 testes automatizados** com **100% de taxa de aprovação**:

### Tecnologias de Teste
- **xUnit**: Executor e framework principal de testes.
- **Moq**: Criação de mocks para isolamento de dependências e gateways.
- **FluentAssertions**: Asserções semânticas e expressivas.
- **Microsoft.AspNetCore.Mvc.Testing (`WebApplicationFactory`)**: Testes de integração de ponta a ponta sem necessidade de servidor externo.
- **EF Core InMemory Database**: Instância de banco em memória isolada e limpa a cada cenário de teste.

### Escopo de Cobertura
* **Testes Unitários de Serviços**: Validação de todas as regras de negócio (RN001 a RN020), incluindo transições de status, hierarquia de equipe, controle de XP/nível e pontuações.
* **Testes de Validadores**: Testes unitários cobrindo validadores do FluentValidation contra payloads nulos, datas passadas, senhas fracas e limites de caracteres.
* **Testes de Controladores & Middleware**: Validação de retorno HTTP correto e formatação RFC 9457 `ProblemDetails` para erros.
* **Testes de Integração**: Testes de disparo de rotas, controle de rate limit, injeção de dependências e verificação de fluxo completo com `CustomWebApplicationFactory`.

### Como Executar os Testes
Para rodar toda a suíte de testes no terminal:

```powershell
# Execução padrão dos testes
dotnet test Cortex.Tests/Cortex.Tests.csproj

# Execução detalhada com saída no console
dotnet test Cortex.Tests/Cortex.Tests.csproj --logger "console;verbosity=detailed"
```

---

## 🛠 Configuração & Execução Local

### Pré-requisitos
* [.NET 10.0 SDK](https://dotnet.microsoft.com/)
* [PostgreSQL](https://www.postgresql.org/) (versão 15 ou superior)

### 1. Clonar o repositório
```bash
git clone https://github.com/aikovlr/Cortex-backend.git
cd Cortex-backend
```

### 2. Configurar Segredos em Desenvolvimento
Utilize o `dotnet user-secrets` para configurar as credenciais locais com segurança:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=Cortex;Username=seu_postgres;Password=sua_senha"
dotnet user-secrets set "Jwt:Secret" "ChaveSecretaDeDesenvolvimentoMuitoSeguraComMinimo32Caracteres!"
```

### 3. Aplicar as Migrations do Banco de Dados
```bash
dotnet ef database update
```

### 4. Executar a Aplicação
```bash
dotnet run
```
* A API iniciará por padrão em `http://localhost:5220`.
* A documentação OpenAPI estará acessível em `http://localhost:5220/openapi/v1.json`.

---

## 📄 Licença
Desenvolvido para fins corporativos e acadêmicos como parte do ecossistema **Cortex**. Todos os direitos reservados.
