# Cortex Backend — Especificação de Endpoints e Contratos

Documentação técnica de todos os endpoints REST da API **Cortex Backend**, contendo formato de requisição, autenticação, payload de entrada (*Request*), payload de saída (*Response*) e status codes esperados.

---

## Sumário
1. [Visão Geral & Autenticação](#visão-geral--autenticação)
2. [Tipos Enumerados (Enums)](#tipos-enumerados-enums)
3. [Autenticação & Acesso (`/api/auth`)](#1-autenticação--acesso-apiauth)
4. [Usuários (`/api/usuario`)](#2-usuários-apiusuario)
5. [Tarefas (`/api/tarefa`)](#3-tarefas-apitarefa)
6. [Equipes & Membros (`/api/equipe`)](#4-equipes--membros-apiequipe)
7. [Metas (`/api/metas` e `/api/tarefas/{id}/metas`)](#5-metas-apimetas)
8. [Sugestões de Alteração (`/api/sugestoes`)](#6-sugestões-de-alteração-apisugestoes)
9. [Tickets de Reporte (`/api/tickets`)](#7-tickets-de-reporte-apitickets)
10. [Respostas de Tarefas (`/api/respostas`)](#8-respostas-de-tarefas-apirespostas)
11. [Anexos (`/api/anexos`)](#9-anexos-apianexos)
12. [Preferências de Notificação (`/api/notificacoes/preferencias`)](#10-preferências-de-notificação-apinotificacoespreferencias)
13. [Relatórios (`/api/relatorios`)](#11-relatórios-apirelatorios)

---

## Visão Geral & Autenticação

- **Base URL**: `http://localhost:5000` (ou porta configurada no ambiente)
- **Content-Type**:
  - `application/json` (padrão)
  - `multipart/form-data` (para endpoints com envio de fotos ou arquivos)
- **Autenticação**:
  - Header: `Authorization: Bearer <seu_token_jwt>`
- **Padronização de Erros**:
  - Respostas de erro seguem o padrão RFC 7807 (`ProblemDetails`):
  ```json
  {
    "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
    "title": "Bad Request",
    "status": 400,
    "detail": "Mensagem detalhada do erro.",
    "instance": "/api/exemplo"
  }
  ```

---

## Tipos Enumerados (Enums)

Podem ser informados como inteiros ou seus nomes como strings:

- **Status** (Ciclo de Vida da Tarefa):
  - `0` ou `"Pendente"`
  - `1` ou `"Atrasada"`
  - `2` ou `"Concluida"`

- **Prioridade**:
  - `0` ou `"Baixa"`
  - `1` ou `"Media"`
  - `2` ou `"Alta"`
  - `3` ou `"Urgente"`

- **Cargo** (Membros de Equipe):
  - `0` ou `"Dono"`
  - `1` ou `"Admin"`
  - `2` ou `"Membro"`

- **StatusSugestao**:
  - `0` ou `"Pendente"`
  - `1` ou `"Aprovada"`
  - `2` ou `"Rejeitada"`

- **CanalNotificacao**:
  - `0` ou `"Email"`
  - `1` ou `"Push"`
  - `2` ou `"Sms"`

- **TipoEntidade** (Anexos):
  - `0` ou `"Tarefa"`
  - `1` ou `"ResponsavelTarefa"`
  - `2` ou `"Usuario"`

---

## 1. Autenticação & Acesso (`/api/auth`)

### 1.1 Login
- **Método / Rota**: `POST /api/auth/login`
- **Autenticação**: Pública (Anônimo)
- **Content-Type**: `application/json`
- **Rate Limit**: Ativo (`login-policy`)
- **Corpo da Requisição (`DadosLoginDTO`)**:
```json
{
  "email": "usuario@exemplo.com",
  "senha": "SenhaForte@123"
}
```
- **Retorno (`200 OK - LoginResponseDTO`)**:
  - **Set-Cookie**: Grava automaticamente o cookie `cortex_token` com as flags `HttpOnly; Secure; SameSite=None`.
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tipo": "Bearer",
  "usuario": {
    "id": 1,
    "nome": "João Silva",
    "email": "usuario@exemplo.com",
    "telefone": "(11) 98765-4321",
    "emailVerificado": true,
    "fotoPerfil": {
      "id": 10,
      "urlCaminho": "/uploads/anexos/foto.jpg",
      "nomeOriginal": "avatar.jpg",
      "mimeType": "image/jpeg",
      "dataEnvio": "2026-10-25T15:00:00Z"
    }
  }
}
```

### 1.2 Logout
- **Método / Rota**: `POST /api/auth/logout`
- **Autenticação**: Pública (Anônimo)
- **Descrição**: Remove/expira o cookie de autenticação `cortex_token`.
- **Retorno**: `204 No Content`

### 1.3 Alterar Senha
- **Método / Rota**: `POST /api/auth/alterar-senha`
- **Autenticação**: Bearer Token Obrigatório (via Cookie HttpOnly ou Header Authorization)
- **Content-Type**: `application/json`
- **Corpo da Requisição (`AlterarSenhaDTO`)**:
```json
{
  "senhaAtual": "SenhaAntiga@123",
  "novaSenha": "NovaSenhaForte@2026"
}
```
- **Retorno**: `204 No Content`

### 1.4 Solicitar Verificação de E-mail (Anônimo)
- **Método / Rota**: `POST /api/auth/solicitar-verificacao-email`
- **Autenticação**: Pública (Anônimo)
- **Rate Limit**: Ativo (`verificacao-email-policy`)
- **Content-Type**: `application/json`
- **Corpo da Requisição (`SolicitarCodigoVerificacaoDTO`)**:
```json
{
  "email": "usuario@exemplo.com"
}
```
- **Retorno (`200 OK`)**:
```json
{
  "message": "Se o e-mail existir, um código foi enviado."
}
```

### 1.4 Validar Código de E-mail (Anônimo)
- **Método / Rota**: `POST /api/auth/verificar-email`
- **Autenticação**: Pública (Anônimo)
- **Content-Type**: `application/json`
- **Corpo da Requisição (`VerificarEmailDTO`)**:
```json
{
  "email": "usuario@exemplo.com",
  "codigo": "123456"
}
```
- **Retorno (`200 OK`)**:
```json
{
  "message": "E-mail verificado com sucesso."
}
```

---

## 2. Usuários (`/api/usuario`)

### 2.1 Criar Usuário (Cadastro)
- **Método / Rota**: `POST /api/usuario`
- **Autenticação**: Pública (Anônimo)
- **Content-Type**: `multipart/form-data`
- **Campos Form-Data (`CriarUsuarioDTO`)**:
  - `Nome` (string, obrigatório)
  - `Email` (string, obrigatório)
  - `Senha` (string, obrigatório - mín. 8 caracteres, maiúscula, minúscula, número e símbolo)
  - `Telefone` (string, opcional)
  - `FotoPerfil` (file, opcional)
- **Retorno (`201 Created - ConsultaUsuarioDTO`)**:
```json
{
  "id": 1,
  "nome": "João Silva",
  "email": "usuario@exemplo.com",
  "telefone": "(11) 98765-4321",
  "emailVerificado": false,
  "fotoPerfil": null
}
```

### 2.2 Listar Usuários
- **Método / Rota**: `GET /api/usuario`
- **Autenticação**: Bearer Token Obrigatório
- **Retorno (`200 OK - List<ConsultaUsuarioDTO>`)**

### 2.3 Obter Usuário por ID
- **Método / Rota**: `GET /api/usuario/{id}`
- **Autenticação**: Bearer Token Obrigatório
- **Retorno (`200 OK - ConsultaUsuarioDTO`)**

### 2.4 Atualizar Usuário por ID
- **Método / Rota**: `PUT /api/usuario/{id}`
- **Autenticação**: Bearer Token Obrigatório (Próprio usuário ou Dono/Admin da mesma equipe)
- **Content-Type**: `multipart/form-data`
- **Campos Form-Data (`AtualizarUsuarioDTO`)**:
  - `Nome` (string, opcional)
  - `Email` (string, opcional)
  - `Senha` (string, opcional)
  - `Telefone` (string, opcional)
  - `FotoPerfil` (file, opcional)
- **Retorno (`200 OK - ConsultaUsuarioDTO`)**

### 2.5 Obter Perfil Autenticado (`/me`)
- **Método / Rota**: `GET /api/usuario/me`
- **Autenticação**: Bearer Token Obrigatório
- **Retorno (`200 OK - ConsultaUsuarioDTO`)**

### 2.6 Atualizar Perfil Autenticado (`/me`)
- **Método / Rota**: `PUT /api/usuario/me`
- **Autenticação**: Bearer Token Obrigatório
- **Content-Type**: `multipart/form-data`
- **Campos Form-Data (`AtualizarUsuarioDTO`)**
- **Retorno (`200 OK - ConsultaUsuarioDTO`)**

### 2.7 Excluir Própria Conta
- **Método / Rota**: `DELETE /api/usuario/minha-conta`
- **Autenticação**: Bearer Token Obrigatório
- **Regra**: Não permite exclusão se houver tarefas `Pendentes` atribuídas ao usuário.
- **Retorno**: `204 No Content`

### 2.8 Excluir Usuário por ID
- **Método / Rota**: `DELETE /api/usuario/{id}`
- **Autenticação**: Bearer Token Obrigatório (Próprio usuário ou Dono/Admin de equipe compartilhada)
- **Retorno**: `204 No Content`

### 2.9 Solicitar Verificação de E-mail (Autenticado)
- **Método / Rota**: `POST /api/usuario/solicitar-verificacao-email`
- **Autenticação**: Bearer Token Obrigatório
- **Retorno (`200 OK`)**:
```json
{
  "message": "Código de verificação enviado para o seu e-mail."
}
```

### 2.10 Confirmar Verificação de E-mail (Autenticado)
- **Método / Rota**: `POST /api/usuario/verificar-email`
- **Autenticação**: Bearer Token Obrigatório
- **Content-Type**: `application/json`
- **Corpo da Requisição (`ConfirmarCodigoDTO`)**:
```json
{
  "codigo": "123456"
}
```
- **Retorno (`200 OK`)**:
```json
{
  "message": "E-mail verificado com sucesso."
}
```

---

## 3. Tarefas (`/api/tarefa`)

### 3.1 Criar Tarefa
- **Método / Rota**: `POST /api/tarefa`
- **Autenticação**: Bearer Token Obrigatório
- **Content-Type**: `multipart/form-data`
- **Campos Form-Data (`CriarTarefaDTO`)**:
  - `Titulo` (string, obrigatório)
  - `Descricao` (string, opcional)
  - `DataVencimento` (DateTime UTC, ISO 8601, obrigatório)
  - `Pontuacao` (int, opcional)
  - `Status` (Status: `Pendente`, `Atrasada`, `Concluida`, obrigatório)
  - `Prioridade` (Prioridade: `Baixa`, `Media`, `Alta`, `Urgente`, obrigatório)
  - `Anexos` (arquivos, opcional)
- **Retorno (`201 Created - DetalhesTarefaDTO`)**:
```json
{
  "id": 1,
  "titulo": "Implementar endpoints da API",
  "descricao": "Criar documentação e testes",
  "dataVencimento": "2026-11-01T18:00:00Z",
  "pontuacao": 100,
  "status": "Pendente",
  "prioridade": "Alta",
  "criadorId": 1,
  "criador": { "id": 1, "nome": "João Silva", "email": "usuario@exemplo.com" },
  "anexos": [],
  "responsaveis": [],
  "respostas": []
}
```

### 3.2 Listar Tarefas (com Filtros)
- **Método / Rota**: `GET /api/tarefa`
- **Autenticação**: Bearer Token Obrigatório
- **Query Params (`FiltroTarefasDTO`)**:
  - `status` (`Pendente`, `Atrasada`, `Concluida`)
  - `prioridade` (`Baixa`, `Media`, `Alta`, `Urgente`)
  - `responsavelId` (int)
  - `criadorId` (int)
  - `dataInicio` (DateTime UTC)
  - `dataFim` (DateTime UTC)
  - `termoBusca` (string)
- **Retorno (`200 OK - List<ConsultaTarefaDTO>`)**:
```json
[
  {
    "id": 1,
    "titulo": "Implementar endpoints da API",
    "descricao": "Criar documentação e testes",
    "dataVencimento": "2026-11-01T18:00:00Z",
    "pontuacao": 100,
    "status": "Pendente",
    "prioridade": "Alta",
    "criadorId": 1
  }
]
```

### 3.3 Obter Detalhes da Tarefa por ID
- **Método / Rota**: `GET /api/tarefa/{id}`
- **Autenticação**: Bearer Token Obrigatório
- **Retorno (`200 OK - DetalhesTarefaDTO`)**

### 3.4 Atualizar Tarefa
- **Método / Rota**: `PUT /api/tarefa/{id}`
- **Autenticação**: Bearer Token Obrigatório (Criador ou Admin/Dono)
- **Content-Type**: `multipart/form-data`
- **Campos Form-Data (`AtualizarTarefaDTO`)**:
  - `Titulo` (string, opcional)
  - `Descricao` (string, opcional)
  - `DataVencimento` (DateTime UTC, opcional)
  - `Pontuacao` (int, opcional)
  - `Prioridade` (Prioridade, opcional)
  - `Anexos` (arquivos, opcional)
- **Retorno (`200 OK - DetalhesTarefaDTO`)**

### 3.5 Excluir Tarefa
- **Método / Rota**: `DELETE /api/tarefa/{id}`
- **Autenticação**: Bearer Token Obrigatório
- **Retorno**: `204 No Content`

### 3.6 Marcar Tarefa como Concluída
- **Método / Rota**: `PATCH /api/tarefa/{id}/concluir`
- **Autenticação**: Bearer Token Obrigatório
- **Retorno (`200 OK - DetalhesTarefaDTO`)**

### 3.7 Atribuir Responsável Individual
- **Método / Rota**: `POST /api/tarefa/{id}/responsaveis`
- **Autenticação**: Bearer Token Obrigatório
- **Content-Type**: `application/json`
- **Corpo da Requisição (`CriarResponsavelTarefaDTO`)**:
```json
{
  "usuarioId": 2
}
```
- **Retorno (`201 Created - ConsultaResponsavelTarefaDTO`)**:
```json
{
  "id": 1,
  "tarefaId": 1,
  "usuarioId": 2,
  "usuario": {
    "id": 2,
    "nome": "Maria Souza",
    "email": "maria@exemplo.com"
  }
}
```

### 3.8 Atribuir Equipe Inteira à Tarefa
- **Método / Rota**: `POST /api/tarefa/{id}/responsaveis/equipe`
- **Autenticação**: Bearer Token Obrigatório
- **Content-Type**: `application/json`
- **Corpo da Requisição (`AtribuirEquipeTarefaDTO`)**:
```json
{
  "equipeId": 3
}
```
- **Retorno (`200 OK - List<ConsultaResponsavelTarefaDTO>`)**

### 3.9 Remover Responsável da Tarefa
- **Método / Rota**: `DELETE /api/tarefa/{id}/responsaveis/{respId}`
- **Autenticação**: Bearer Token Obrigatório
- **Retorno**: `204 No Content`

### 3.10 Listar Responsáveis da Tarefa
- **Método / Rota**: `GET /api/tarefa/{id}/responsaveis`
- **Autenticação**: Bearer Token Obrigatório
- **Retorno (`200 OK - List<ConsultaResponsavelTarefaDTO>`)**

---

## 4. Equipes & Membros (`/api/equipe`)

### 4.1 Criar Equipe
- **Método / Rota**: `POST /api/equipe`
- **Autenticação**: Bearer Token Obrigatório (O criador se torna o `Dono`)
- **Content-Type**: `application/json`
- **Corpo da Requisição (`CriarEquipeDTO`)**:
```json
{
  "nome": "Time de Engenharia"
}
```
- **Retorno (`201 Created - ConsultaEquipeDTO`)**:
```json
{
  "id": 1,
  "nome": "Time de Engenharia",
  "dataCriacao": "2026-10-25T15:00:00Z",
  "membros": [
    {
      "id": 1,
      "usuarioId": 1,
      "equipeId": 1,
      "cargo": "Dono",
      "dataEntrada": "2026-10-25T15:00:00Z"
    }
  ]
}
```

### 4.2 Listar Minhas Equipes
- **Método / Rota**: `GET /api/equipe`
- **Autenticação**: Bearer Token Obrigatório
- **Retorno (`200 OK - List<ConsultaEquipeDTO>`)**

### 4.3 Obter Equipe por ID
- **Método / Rota**: `GET /api/equipe/{id}`
- **Autenticação**: Bearer Token Obrigatório
- **Retorno (`200 OK - ConsultaEquipeDTO`)**

### 4.4 Atualizar Equipe
- **Método / Rota**: `PUT /api/equipe/{id}`
- **Autenticação**: Bearer Token Obrigatório (Dono ou Admin)
- **Content-Type**: `application/json`
- **Corpo da Requisição (`AtualizarEquipeDTO`)**:
```json
{
  "nome": "Time de Engenharia & Produto"
}
```
- **Retorno (`200 OK - ConsultaEquipeDTO`)**

### 4.5 Deletar Equipe
- **Método / Rota**: `DELETE /api/equipe/{id}`
- **Autenticação**: Bearer Token Obrigatório (Apenas Dono)
- **Retorno**: `204 No Content`

### 4.6 Adicionar Membro à Equipe
- **Método / Rota**: `POST /api/equipe/{id}/membros`
- **Autenticação**: Bearer Token Obrigatório (Dono ou Admin)
- **Content-Type**: `application/json`
- **Corpo da Requisição (`CriarMembroEquipeDTO`)**:
```json
{
  "usuarioId": 2,
  "cargo": "Membro"
}
```
- **Retorno (`201 Created - ConsultaMembroEquipeDTO`)**:
```json
{
  "id": 2,
  "usuarioId": 2,
  "equipeId": 1,
  "cargo": "Membro",
  "dataEntrada": "2026-10-25T15:30:00Z",
  "usuario": {
    "id": 2,
    "nome": "Maria Souza",
    "email": "maria@exemplo.com"
  }
}
```

### 4.7 Remover Membro da Equipe
- **Método / Rota**: `DELETE /api/equipe/{id}/membros/{membroId}`
- **Autenticação**: Bearer Token Obrigatório (Dono ou Admin)
- **Retorno**: `204 No Content`

### 4.8 Alterar Cargo de Membro
- **Método / Rota**: `PATCH /api/equipe/{id}/membros/{membroId}/cargo`
- **Autenticação**: Bearer Token Obrigatório (Apenas Dono)
- **Content-Type**: `application/json`
- **Corpo da Requisição (`AtualizarMembroEquipeDTO`)**:
```json
{
  "cargo": "Admin"
}
```
- **Retorno (`200 OK - ConsultaMembroEquipeDTO`)**

### 4.9 Listar Membros da Equipe
- **Método / Rota**: `GET /api/equipe/{id}/membros`
- **Autenticação**: Bearer Token Obrigatório
- **Retorno (`200 OK - List<ConsultaMembroEquipeDTO>`)**

---

## 5. Metas (`/api/metas`)

### 5.1 Criar Meta Global
- **Método / Rota**: `POST /api/metas`
- **Autenticação**: Bearer Token Obrigatório
- **Content-Type**: `application/json`
- **Corpo da Requisição (`CriarMetaDTO`)**:
```json
{
  "nome": "Lançamento da v1.0",
  "dataFechamento": "2026-12-31T23:59:59Z",
  "tarefaId": 1
}
```
- **Retorno (`201 Created - ConsultaMetaDTO`)**:
```json
{
  "id": 1,
  "nome": "Lançamento da v1.0",
  "dataFechamento": "2026-12-31T23:59:59Z",
  "concluida": false,
  "tarefaId": 1,
  "usuarioId": 1,
  "tarefa": { "id": 1, "titulo": "Implementar endpoints da API" },
  "usuario": { "id": 1, "nome": "João Silva" }
}
```

### 5.2 Criar Meta Vinculada à Tarefa
- **Método / Rota**: `POST /api/tarefas/{id}/metas`
- **Autenticação**: Bearer Token Obrigatório
- **Content-Type**: `application/json`
- **Corpo da Requisição (`CriarMetaDTO`)**:
```json
{
  "nome": "Documentação Finalizada",
  "dataFechamento": "2026-11-15T23:59:59Z"
}
```
- **Retorno (`201 Created - ConsultaMetaDTO`)**

### 5.3 Listar Metas por Usuário
- **Método / Rota**: `GET /api/metas`
- **Autenticação**: Bearer Token Obrigatório
- **Query Params**:
  - `usuarioId` (int, opcional — se omitido, retorna as metas do usuário logado)
- **Retorno (`200 OK - List<ConsultaMetaDTO>`)**

### 5.4 Listar Metas por Tarefa
- **Método / Rota**: `GET /api/tarefas/{id}/metas`
- **Autenticação**: Bearer Token Obrigatório
- **Retorno (`200 OK - List<ConsultaMetaDTO>`)**

### 5.5 Obter Meta por ID
- **Método / Rota**: `GET /api/metas/{id}`
- **Autenticação**: Bearer Token Obrigatório
- **Retorno (`200 OK - ConsultaMetaDTO`)**

### 5.6 Atualizar Meta
- **Método / Rota**: `PUT /api/metas/{id}`
- **Autenticação**: Bearer Token Obrigatório
- **Content-Type**: `application/json`
- **Corpo da Requisição (`AtualizarMetaDTO`)**:
```json
{
  "nome": "Lançamento da v1.0 atualizado",
  "dataFechamento": "2027-01-31T23:59:59Z",
  "concluida": false,
  "tarefaId": 1,
  "usuarioId": 1
}
```
- **Retorno (`200 OK - ConsultaMetaDTO`)**

### 5.7 Deletar Meta
- **Método / Rota**: `DELETE /api/metas/{id}`
- **Autenticação**: Bearer Token Obrigatório
- **Retorno**: `204 No Content`

---

## 6. Sugestões de Alteração (`/api/sugestoes`)

### 6.1 Submeter Sugestão para uma Tarefa
- **Método / Rota**: `POST /api/tarefas/{id}/sugestoes`
- **Autenticação**: Bearer Token Obrigatório
- **Content-Type**: `application/json`
- **Corpo da Requisição (`CriarSugestaoDTO`)**:
```json
{
  "descricao": "Sugiro prorrogar o prazo de vencimento em 3 dias."
}
```
- **Retorno (`201 Created - ConsultaSugestaoDTO`)**:
```json
{
  "id": 1,
  "descricao": "Sugiro prorrogar o prazo de vencimento em 3 dias.",
  "dataCriacao": "2026-10-25T16:00:00Z",
  "status": "Pendente",
  "tarefaId": 1,
  "usuarioId": 2,
  "usuario": { "id": 2, "nome": "Maria Souza" },
  "tarefa": { "id": 1, "titulo": "Implementar endpoints da API" }
}
```

### 6.2 Listar Sugestões de uma Tarefa
- **Método / Rota**: `GET /api/tarefas/{id}/sugestoes`
- **Autenticação**: Bearer Token Obrigatório
- **Retorno (`200 OK - List<ConsultaSugestaoDTO>`)**

### 6.3 Aprovar Sugestão
- **Método / Rota**: `PATCH /api/sugestoes/{id}/aprovar`
- **Autenticação**: Bearer Token Obrigatório (Apenas o Criador da tarefa)
- **Retorno (`200 OK - ConsultaSugestaoDTO`)** (`status: "Aprovada"`)

### 6.4 Rejeitar Sugestão
- **Método / Rota**: `PATCH /api/sugestoes/{id}/rejeitar`
- **Autenticação**: Bearer Token Obrigatório (Apenas o Criador da tarefa)
- **Retorno (`200 OK - ConsultaSugestaoDTO`)** (`status: "Rejeitada"`)

---

## 7. Tickets de Reporte (`/api/tickets`)

### 7.1 Abrir Ticket de Problema em Tarefa
- **Método / Rota**: `POST /api/tarefas/{id}/tickets`
- **Autenticação**: Bearer Token Obrigatório
- **Content-Type**: `application/json`
- **Corpo da Requisição (`CriarTicketReporteDTO`)**:
```json
{
  "descricao": "Servidor de banco de homologação está inacessível impedindo os testes."
}
```
- **Retorno (`201 Created - ConsultaTicketReporteDTO`)**:
```json
{
  "id": 1,
  "descricao": "Servidor de banco de homologação está inacessível impedindo os testes.",
  "ativo": true,
  "dataCriacao": "2026-10-25T16:15:00Z",
  "tarefaId": 1,
  "usuarioId": 2,
  "usuario": { "id": 2, "nome": "Maria Souza" },
  "tarefa": { "id": 1, "titulo": "Implementar endpoints da API" }
}
```

### 7.2 Listar Tickets de uma Tarefa
- **Método / Rota**: `GET /api/tarefas/{id}/tickets`
- **Autenticação**: Bearer Token Obrigatório
- **Retorno (`200 OK - List<ConsultaTicketReporteDTO>`)**

### 7.3 Fechar / Resolver Ticket
- **Método / Rota**: `PATCH /api/tickets/{id}/fechar`
- **Autenticação**: Bearer Token Obrigatório (Criador do ticket ou Dono/Admin da equipe)
- **Retorno (`200 OK - ConsultaTicketReporteDTO`)** (`ativo: false`)

### 7.4 Listar Tickets Abertos por Equipe
- **Método / Rota**: `GET /api/equipes/{id}/tickets`
- **Autenticação**: Bearer Token Obrigatório (Apenas Dono/Admin da equipe)
- **Retorno (`200 OK - List<ConsultaTicketReporteDTO>`)**

---

## 8. Respostas de Tarefas (`/api/respostas`)

### 8.1 Publicar Resposta / Comentário em Tarefa
- **Método / Rota**: `POST /api/tarefas/{id}/respostas`
- **Autenticação**: Bearer Token Obrigatório
- **Content-Type**: `multipart/form-data`
- **Campos Form-Data (`CriarRespostaTarefaDTO`)**:
  - `Anexos` (arquivos, opcional)
- **Retorno (`201 Created - ConsultaRespostaTarefaDTO`)**:
```json
{
  "id": 1,
  "dataResposta": "2026-10-25T16:20:00Z",
  "usuarioId": 1,
  "tarefaId": 1,
  "usuario": { "id": 1, "nome": "João Silva" },
  "anexos": [
    {
      "id": 2,
      "urlCaminho": "/uploads/anexos/evidencia.pdf",
      "nomeOriginal": "evidencia.pdf",
      "mimeType": "application/pdf",
      "dataEnvio": "2026-10-25T16:20:00Z",
      "tipoEntidade": "Tarefa"
    }
  ]
}
```

### 8.2 Listar Respostas de uma Tarefa
- **Método / Rota**: `GET /api/tarefas/{id}/respostas`
- **Autenticação**: Bearer Token Obrigatório
- **Retorno (`200 OK - List<ConsultaRespostaTarefaDTO>`)**

### 8.3 Deletar Resposta
- **Método / Rota**: `DELETE /api/respostas/{id}`
- **Autenticação**: Bearer Token Obrigatório (Autor da resposta ou Dono/Admin)
- **Retorno**: `204 No Content`

---

## 9. Anexos (`/api/anexos`)

### 9.1 Upload de Anexos em Tarefa
- **Método / Rota**: `POST /api/tarefas/{id}/anexos`
- **Autenticação**: Bearer Token Obrigatório
- **Content-Type**: `multipart/form-data`
- **Campos Form-Data**:
  - `arquivos` (lista de `IFormFile`, obrigatório)
- **Retorno (`201 Created - List<ConsultaAnexoDTO>`)**:
```json
[
  {
    "id": 3,
    "urlCaminho": "/uploads/anexos/diagrama.png",
    "nomeOriginal": "diagrama.png",
    "mimeType": "image/png",
    "dataEnvio": "2026-10-25T16:30:00Z",
    "tipoEntidade": "Tarefa",
    "tarefaId": 1
  }
]
```

### 9.2 Listar Anexos de uma Tarefa
- **Método / Rota**: `GET /api/tarefas/{id}/anexos`
- **Autenticação**: Bearer Token Obrigatório
- **Retorno (`200 OK - List<ConsultaAnexoDTO>`)**

### 9.3 Obter Anexo por ID
- **Método / Rota**: `GET /api/anexos/{id}`
- **Autenticação**: Bearer Token Obrigatório
- **Retorno (`200 OK - ConsultaAnexoDTO`)**

### 9.4 Listar Anexos por Entidade Genérica
- **Método / Rota**: `GET /api/entidades/{tipoEntidade}/anexos/{entidadeId}`
- **Autenticação**: Bearer Token Obrigatório
- **Parâmetros de Rota**:
  - `tipoEntidade`: `Tarefa`, `ResponsavelTarefa` ou `Usuario`
  - `entidadeId`: int (ID da entidade)
- **Retorno (`200 OK - List<ConsultaAnexoDTO>`)**

### 9.5 Deletar Anexo
- **Método / Rota**: `DELETE /api/anexos/{id}`
- **Autenticação**: Bearer Token Obrigatório
- **Retorno**: `204 No Content`

---

## 10. Preferências de Notificação (`/api/notificacoes/preferencias`)

### 10.1 Obter Preferências do Usuário Autenticado
- **Método / Rota**: `GET /api/notificacoes/preferencias`
- **Autenticação**: Bearer Token Obrigatório
- **Retorno (`200 OK - List<PreferenciaNotificacaoDTO>`)**:
```json
[
  {
    "id": 1,
    "usuarioId": 1,
    "canal": "Email",
    "antecedenciaHoras": 24,
    "ativoNovaAtribuicao": true,
    "ativoVencimento": true,
    "ativoConclusaoMeta": true,
    "ativoTicket": true
  }
]
```

### 10.2 Configurar Preferência de Notificação por Canal
- **Método / Rota**: `PUT /api/notificacoes/preferencias`
- **Autenticação**: Bearer Token Obrigatório
- **Content-Type**: `application/json`
- **Regra**: Notificação por e-mail exige conta com e-mail confirmado (`EmailVerificado = true`). Antecedência configurável entre 1 e 168 horas (1 a 7 dias).
- **Corpo da Requisição (`ConfigurarPreferenciaDTO`)**:
```json
{
  "canal": "Email",
  "antecedenciaHoras": 48,
  "ativoNovaAtribuicao": true,
  "ativoVencimento": true,
  "ativoConclusaoMeta": false,
  "ativoTicket": true
}
```
- **Retorno (`200 OK - PreferenciaNotificacaoDTO`)**

---

## 11. Relatórios (`/api/relatorios`)

### 11.1 Gerar Relatório de Tarefas
- **Método / Rota**: `GET /api/relatorios/tarefas`
- **Autenticação**: Bearer Token Obrigatório (Usuários com cargo `Membro` visualizam somente suas próprias tarefas)
- **Query Params (`FiltroRelatorioDTO`)**:
  - `status` (`Pendente`, `Atrasada`, `Concluida`)
  - `prioridade` (`Baixa`, `Media`, `Alta`, `Urgente`)
  - `equipeId` (int)
  - `responsavelId` (int)
  - `dataInicio` (DateTime UTC)
  - `dataFim` (DateTime UTC)
- **Retorno (`200 OK - RelatorioTarefasDTO`)**:
```json
{
  "totalTarefas": 15,
  "concluidas": 10,
  "pendentes": 4,
  "atrasadas": 1,
  "taxaConclusaoPercentual": 66.67,
  "porPrioridade": {
    "Alta": 5,
    "Media": 7,
    "Baixa": 3
  },
  "porStatus": {
    "Concluidas": 10,
    "Pendentes": 4,
    "Atrasadas": 1
  },
  "periodoInicio": "2026-10-01T00:00:00Z",
  "periodoFim": "2026-10-31T23:59:59Z",
  "geradoEm": "2026-10-25T16:45:00Z",
  "tarefas": [
    {
      "id": 1,
      "titulo": "Implementar endpoints da API",
      "dataVencimento": "2026-11-01T18:00:00Z",
      "status": "Pendente",
      "prioridade": "Alta"
    }
  ]
}
```

### 11.2 Gerar Relatório Consolidado de Equipe
- **Método / Rota**: `GET /api/relatorios/equipes/{id}`
- **Autenticação**: Bearer Token Obrigatório (Apenas `Dono` ou `Admin` da equipe)
- **Query Params (`FiltroRelatorioDTO`)**:
  - `status`, `prioridade`, `responsavelId`, `dataInicio`, `dataFim`
- **Retorno (`200 OK - RelatorioEquipeDTO`)**:
```json
{
  "equipeId": 1,
  "equipeNome": "Time de Engenharia",
  "totalMembros": 5,
  "metricasGerais": {
    "totalTarefas": 30,
    "concluidas": 20,
    "pendentes": 8,
    "atrasadas": 2,
    "taxaConclusaoPercentual": 66.67,
    "porPrioridade": { "Alta": 10, "Media": 15, "Baixa": 5 },
    "porStatus": { "Concluidas": 20, "Pendentes": 8, "Atrasadas": 2 },
    "geradoEm": "2026-10-25T16:45:00Z",
    "tarefas": []
  },
  "desempenhoMembros": [
    {
      "usuarioId": 2,
      "nome": "Maria Souza",
      "email": "maria@exemplo.com",
      "cargo": "Membro",
      "totalAtribuidas": 10,
      "concluidas": 8,
      "pendentes": 2,
      "atrasadas": 0,
      "taxaConclusaoPercentual": 80.0
    }
  ]
}
```
