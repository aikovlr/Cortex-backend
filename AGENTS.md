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

---

## 3. Requisitos Funcionais (RFs)

| Código | Descrição | Implementado? |
| :--- | :--- | :--- |
| **RF001** | O sistema deve permitir cadastrar, consultar, atualizar e excluir usuários, autenticar por e-mail e senha, verificar o e-mail por código de seis dígitos e solicitar/redefinir senha por código enviado ao e-mail. Dados: nome, e-mail, telefone e foto de perfil; a senha e os códigos não devem ser expostos em consultas. | Sim |
| **RF002** | O sistema deve permitir criar, consultar, editar, excluir, concluir e reabrir tarefas, bem como atribuir e remover responsáveis individualmente ou por todos os membros de uma equipe. Dados: título, descrição, vencimento, pontuação, status, prioridade, criador, responsáveis e anexos. | Sim |
| **RF003** | O sistema deve permitir que um responsável envie entregas de uma tarefa, com arquivos anexos, e que o criador avalie cada entrega com uma nota. Dados: tarefa, responsável, data de envio, nota e anexos. | Sim |
| **RF004** | O sistema deve permitir que responsáveis submetam sugestões de alteração de uma tarefa e que seu criador as aprove ou rejeite. Dados: tarefa, solicitante, descrição, data de criação e status. | Sim |
| **RF005** | O sistema deve permitir que responsáveis abram tickets de impedimento em tarefas não concluídas, que o criador seja notificado e que o criador encerre o ticket. Dados: tarefa, autor, descrição, data de abertura e situação ativa/fechada. | Sim |
| **RF006** | O sistema deve permitir criar, consultar, alterar e excluir equipes, bem como incluir, remover e listar membros. O criador torna-se dono; somente o dono pode alterar cargos, e dono/admin podem administrar membros. Dados: nome da equipe, data de criação, membro, cargo e dados de contato do usuário. | Sim |
| **RF007** | O sistema deve permitir criar, consultar, alterar e excluir metas associadas a uma ou mais tarefas e a um usuário proprietário ou pertencente a uma equipe, calculando a conclusão conforme o status das tarefas vinculadas. O sistema deve atribuir ou remover pontos de experiência (XP) do nível da conta dos usuários quando uma meta é concluída ou passa do prazo de vencimento. Dados: nome, prazo de fechamento, proprietário, conclusão, tarefas vinculadas e XP de recompensa (apenas para equipes). | Sim |
| **RF008** | O sistema deve permitir configurar, por canal (e-mail, push e SMS), a antecedência de alertas e a ativação de notificações para nova atribuição, vencimento, conclusão de meta e ticket. Dados: canal, antecedência e indicadores de ativação por evento. | Parcialmente, somente o canal de notificação de e-mail foi implementado. |
| **RF009** | O sistema deve disponibilizar uma central de notificações no cabeçalho para listar notificações do usuário, indicar a quantidade de não lidas, marcar uma ou todas como lidas e remover notificações individuais ou o histórico. Dados: tipo, título, mensagem, link, data, leitura e destaque. | Sim |
| **RF010** | O sistema deve identificar tarefas pendentes vencidas e atualizá-las para “Atrasada”, além de alertar responsáveis quando uma tarefa estiver dentro da antecedência configurada. O registro de alertas deve impedir reenvio para o mesmo usuário, tarefa e canal dentro da janela aplicável. | Sim |
| **RF011** | O sistema deve gerar relatórios de tarefas com filtros por status, prioridade, período, responsável, criador e equipe. Usuários comuns devem acessar apenas seus próprios dados; donos/admins devem acessar relatórios das equipes autorizadas. Métricas: total, contagens por status, taxa de conclusão, distribuição por prioridade, média de notas e desempenho por responsável. | Sim |
| **RF012** | O sistema deve permitir anexar, consultar e excluir arquivos relacionados a tarefas, entregas e perfis de usuário, disponibilizando visualização ou download quando suportado. Dados: nome original, tipo MIME, caminho/URL, data de envio e entidade relacionada. | Sim |
| **RF013** | O sistema deve permitir ao usuário configurar e persistir tema claro/escuro, alto contraste e tamanho de fonte entre 12 e 24 pixels, em valores pares. | Sim |

---

## 4. Requisitos Não Funcionais (RNFs)

| Código | Descrição |
| :--- | :--- |
| **RNF001** | O sistema deve garantir a segurança e proteção dos dados dos usuários, incluindo criptografia de senhas e controle de acesso por níveis de permissão. |
| **RNF002** | O sistema deve apresentar uma interface amigável, responsiva e acessível a usuários com diferentes níveis de familiaridade tecnológica. |
| **RNF003** | O sistema deve ser compatível com os principais navegadores e dispositivos móveis. |
| **RNF004** | O sistema deve suportar múltiplos acessos simultâneos sem comprometer a performance. |
| **RNF005** | O sistema deve manter logs de atividades relevantes para auditoria e rastreabilidade de ações críticas (como login, alterações de tarefas e exclusão de dados). |
| **RNF006** | O sistema deve realizar backups periódicos para garantir a integridade e disponibilidade dos dados. |
| **RNF007** | O sistema deve utilizar um banco de dados relacional (PostgreSQL) para armazenar informações estruturadas de forma segura e eficiente. |
| **RNF008** | O sistema deve seguir boas práticas de desenvolvimento seguro, incluindo validação de dados de entrada, controle de erros e prevenção de vulnerabilidades conhecidas. |
| **RNF009** | O tempo de resposta para ações críticas (como salvar, editar ou excluir tarefas) não deve ultrapassar 2 segundos. |
| **RNF010** | O sistema deve processar notificações de forma assíncrona e encaminhá-las ao provedor configurado conforme o canal selecionado. Para aplicações web, o envio push poderá utilizar Web Push; para dispositivos Android, Firebase Cloud Messaging (FCM); e, para dispositivos Apple, Apple Push Notification service (APNs). |

---

## 5. Regras de Negócio (RNs)

| Código | Descrição | Vinculada ao RF |
| :--- | :--- | :--- |
| **RN001** | Um e-mail só pode estar vinculado a uma única conta no sistema. | RF001 |
| **RN002** | A senha deve conter no mínimo 8 caracteres, incluindo letras maiúsculas, minúsculas, números e símbolos. | RF001 |
| **RN003** | Somente donos de equipes podem alterar o cargo de outro usuário. | RF006 |
| **RN004** | Somente donos e administradores podem adicionar ou remover usuários. | RF006 |
| **RN005** | Um usuário não pode excluir a própria conta se houver tarefas com status ‘Pendente’ ou ‘Atrasada’ atribuídas a ele. | RF001, RF002 |
| **RN006** | Uma tarefa vencida e não concluída muda automaticamente para o status ‘Atrasada’ e notifica o responsável. | RF010 |
| **RN007** | Cada meta deve estar vinculada a no mínimo uma tarefa para ser considerada válida. | RF007 |
| **RN008** | O sistema só contabiliza uma meta como ‘Concluida’ se todas as tarefas relacionadas estiverem concluídas. | RF007 |
| **RN009** | Usuários comuns só podem gerar relatórios de suas próprias tarefas. | RF011 |
| **RN010** | Donos e administradores de equipe podem gerar relatórios por equipe. | RF011 |
| **RN011** | O usuário deve configurar a antecedência mínima para receber notificações de expiração de tarefas (mínimo: 1 hora, máximo: 7 dias). | RF008 |
| **RN012** | Para notificações por e-mail, o usuário deve confirmar o endereço antes de ativar o envio. | RF001, RF008 |
| **RN013** | Sugestões de alteração de tarefa devem ser aprovadas ou recusadas pelo criador antes de serem aplicadas. | RF004 |
| **RN014** | Metas devem ter validade definida pelo usuário com prazo máximo de 12 meses. | RF007 |
| **RN015** | Entregas de tarefas só podem ser avaliadas com nota pelo criador da tarefa. | RF003 |
| **RN016** | Tickets de reporte só podem ser abertos em tarefas não concluídas e encerrados pelo criador da tarefa. | RF005 |
| **RN017** | O sistema deve bloquear o reenvio de alertas de vencimento para o mesmo usuário, tarefa e canal dentro da janela de antecedência configurada. | RF010 |
| **RN018** | A configuração de tamanho de fonte por acessibilidade deve aceitar exclusivamente valores pares entre 12 e 24 pixels. | RF013 |
| **RN019** | O sistema deve incrementar o saldo de experiência (XP) do usuário sempre que uma meta de equipe for marcada como concluída. Cada tarefa com status ‘Concluída’ deve incrementar o saldo de XP em 5. Quando o limite de XP daquele nível for atingido, o sistema deve aumentar o nível da conta em +1 e manter o saldo de XP excedente para o próximo nível. Metas puramente individuais não devem gerar acúmulo de XP. | RF007 |
| **RN020** | O sistema deve reduzir o saldo de experiência (XP) do usuário sempre que uma meta que possui prazo de vencimento expirar. Para cada tarefa vinculada a meta expirada com status de ‘Pendente’ ou ‘Atrasadas’, o sistema deve reduzir o saldo de XP em -5. Tarefas com status ‘Concluída’ não alteram o saldo. | RF007 |

---

## 6. Instruções e Convenções para Desenvolvimento

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
