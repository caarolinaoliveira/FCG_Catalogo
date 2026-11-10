# FCG Catálogo

Microsserviço responsável pelo gerenciamento de jogos e fluxo de compra da plataforma FIAP Cloud Games.

## Responsabilidades

- CRUD de jogos
- Busca de jogos com tolerância a erros de digitação e ordenação por relevância (Elasticsearch)
- Iniciar fluxo de compra publicando um pedido
- Adicionar jogo à biblioteca do usuário quando pagamento aprovado

## Eventos publicados

| Evento | Fila | Quando |
|--------|------|--------|
| `OrderPlacedEvent` | `order.placed` | Usuário solicita compra de jogo |

## Eventos consumidos

| Evento | Fila | Ação |
|--------|------|------|
| `PaymentProcessedEvent` | `payment.processed` | Adiciona jogo à biblioteca se aprovado |

> **Implantação:** o consumidor de `payment.processed` já roda **dentro da API** (a imagem Docker publica só o `FCG.Catalogo.Presentation`). O projeto `FCG.Catalogo.Worker` registra o mesmo consumidor em outro host e **não deve ser implantado junto com a API**, senão as duas cópias disputam as mensagens da mesma fila.

## Health check

`GET /health` responde `200 Healthy` quando o processo está de pé. É usado pelas probes de readiness e liveness do Kubernetes.

## Autorização

O JWT é emitido pelo serviço de Usuários (`POST /api/usuarios/login`) e validado aqui. As roles vêm na claim `role`: **`Usuario`** (todo usuário registrado) e **`Admin`**. A API é **segura por padrão**: qualquer rota sem regra explícita exige usuário autenticado.

| Rota | Regra |
|---|---|
| `GET /api/jogos`, `GET /api/jogos/{id}`, `GET /api/jogos/buscar/{titulo}` e `GET /api/jogos/search` (busca fuzzy) | Pública |
| `POST /api/jogos/cadastrar`, `PUT /api/jogos/{id}`, `DELETE /api/jogos/{id}` | Role **`Admin`** (sem token: 401; sem a role: 403) |
| `GET` e `POST /api/jogos/{jogoId}/avaliacoes` | Usuário autenticado |
| `GET /api/biblioteca/{usuarioId}` | Usuário autenticado |
| `POST /api/pedidos/{jogoId}`, `GET /api/pedidos/usuario/{usuarioId}` | Usuário autenticado |
| `GET /health` | Pública (as probes do Kubernetes não enviam token) |

**Limitação conhecida:** o token só traz o e-mail e a role, não o id do usuário. Por isso as rotas que recebem `usuarioId` (biblioteca, pedidos e avaliações) exigem login, mas **não verificam** se o `usuarioId` informado pertence a quem está logado. Para fechar isso, o serviço de Usuários precisa colocar o id do usuário no token.

**Como obter um administrador.** Não existe seed nem endpoint que crie um `Admin`: o `registrar` sempre atribui só `Usuario`. Registre o usuário normalmente e promova-o por SQL no banco `FCG_Usuarios`:

```sql
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT u.Id, r.Id FROM AspNetUsers u, AspNetRoles r
WHERE u.Email = 'admin@example.com' AND r.Name = 'Admin';
```

As roles ficam em cache por 5 minutos no login do serviço de Usuários: promova o usuário antes do primeiro login dele (ou aguarde a expiração).

## Busca (Elasticsearch)

`GET /api/jogos/search?q=<texto>&pagina=1&tamanho=10`: busca **pública** (sem token) com correspondência aproximada (**fuzzy**: tolera erros de digitação) e resultados **ordenados por relevância**.

| Parâmetro | Obrigatório | Padrão | Regra |
|---|---|---|---|
| `q` | sim | | Texto da busca. Vazio ou ausente: `400` |
| `pagina` | não | `1` | Mínimo 1 |
| `tamanho` | não | `10` | Entre 1 e 50 |

| Resposta | Quando |
|---|---|
| `200` | Busca realizada (mesmo sem resultado: `total` 0 e `itens` vazio) |
| `400` | Parâmetro inválido |
| `503` | Elasticsearch indisponível. O CRUD de jogos **não** é afetado |

```json
{
  "pagina": 1,
  "tamanho": 10,
  "total": 4,
  "itens": [
    { "id": "…", "titulo": "Aventura Submarina", "descricao": "Explore o fundo do mar", "genero": "Aventura",
      "preco": 49.90, "precoPromocional": null, "dataLancamento": "2026-01-01T00:00:00", "score": 3.81 },
    { "id": "…", "titulo": "Corrida Radical", "descricao": "…com muita aventura", "genero": "Acao",
      "preco": 59.90, "precoPromocional": null, "dataLancamento": "2026-01-01T00:00:00", "score": 0.91 }
  ]
}
```

**Como a relevância é calculada.** Uma consulta `multi_match` nos campos `titulo^3` e `descricao`, com `fuzziness: AUTO`, ordenada por `_score` decrescente. O título pesa **3 vezes** mais que a descrição: para `q=aventra`, os jogos com a palavra no título vêm antes dos que só a citam na descrição. O `score` de cada item é devolvido.

| Tamanho do termo | Erros tolerados (distância de edição) |
|---|---|
| 1 a 2 letras | 0 (exato) |
| 3 a 5 letras | 1 |
| 6 letras ou mais | 2 |

A busca também ignora **maiúsculas e acentos** (analisador `texto` = tokenizer `standard` + `lowercase` + `asciifolding`): `dragoes` acha "Mestre dos Dragões", e `aventra` acha "Aventura".

**Índice `jogos`** (mapeamento explícito, 1 shard e 0 réplicas, pois há um único nó):

| Campo | Tipo |
|---|---|
| `id` | `keyword` |
| `titulo` | `text` (analisador `texto`) + subcampo `titulo.keyword` |
| `descricao` | `text` (analisador `texto`) |
| `genero` | `keyword` (nome do gênero) |
| `preco`, `precoPromocional` | `scaled_float` (fator 100) |
| `dataLancamento` | `date` |

**Como o índice é mantido em dia.**

- **CRUD:** criar e editar indexam o jogo, e excluir remove o documento, exatamente nos mesmos pontos que invalidam o cache do Redis (`JogoService`). A indexação é *best effort*: se o Elasticsearch estiver fora do ar, o erro é só **registrado no log** e o CRUD segue normalmente. Depois de uma falha, um circuito de 30s evita pagar o timeout a cada requisição. Um jogo fica visível na busca em cerca de 1 segundo.
- **Reconciliação:** no startup e a cada 60s, o Catálogo compara o índice com o SQL Server. Se o índice não existir ou tiver uma contagem diferente da do SQL, ele é **recriado e todos os jogos são reindexados em lote (bulk de 500)**. Isso é necessário porque, no cluster, o Elasticsearch usa `emptyDir`: quando o pod dele reinicia, o índice some enquanto o Catálogo continua rodando. A reconciliação também corrige o índice depois de um período em que o Elasticsearch esteve fora do ar.
- **Limitação:** a verificação compara a **quantidade** de documentos. Se uma *edição* se perder enquanto o Elasticsearch está fora do ar (sem mudar o número de jogos), o título antigo continua no índice até o próximo rebuild (por exemplo, o próximo restart do Catálogo).

Uso de memória medido no cluster local (índice de 5.000 jogos, 400 buscas e um pico de 8 requisições simultâneas): o pod do Elasticsearch ficou em cerca de **810 Mi de memória anônima**, com o heap em no máximo 213 Mi dos 384 Mi.

## Swagger

| Chave | Padrão | Efeito |
|---|---|---|
| `Swagger:Habilitado` (`Swagger__Habilitado`) | `false` | Liga o Swagger fora do ambiente `Development`. Em `Development` ele está sempre ligado |
| `Swagger:RoutePrefix` (`Swagger__RoutePrefix`) | `swagger` | Caminho da UI e do JSON (`/<prefixo>/index.html`, `/<prefixo>/v1/swagger.json`) |

A UI usa a URL do JSON **relativa** (`v1/swagger.json`) e o documento não define `servers`, então funciona em qualquer caminho e atrás de um Ingress **sem rewrite**. Atrás do Ingress, `/swagger` já é usado pelo serviço de Usuários, por isso o Catálogo usa outro prefixo (ex.: `Swagger__RoutePrefix=swagger-catalogo`) e o Ingress precisa de uma regra para ele. O botão **Authorize** aceita o token JWT (esquema `Bearer`) obtido em `POST /api/usuarios/login`.

> O Swagger documenta todas as rotas da API. Deixe `Swagger:Habilitado=false` quando não for necessário.

O arquivo [`FCG.Catalogo.Presentation.http`](FCG.Catalogo.Presentation/FCG.Catalogo.Presentation.http) traz a sequência de demonstração (login de usuário e de administrador, a segurança em ação com 401 e 403, CRUD de jogos, cache e o placeholder da busca fuzzy).

## Variáveis de ambiente

| Variável | Obrigatória | Descrição | Padrão |
|----------|-------------|-----------|--------|
| `ConnectionStrings__DefaultConnection` | **sim** | Connection string do SQL Server (banco `FCG_Catalogo`) | - |
| `JwtSettings__Segredo` | **sim** (só na API) | Chave de assinatura do JWT (mínimo 32 caracteres; o mesmo valor do serviço de Usuários) | - |
| `RabbitMQ__Usuario` | **sim** | Usuário do RabbitMQ | - |
| `RabbitMQ__Senha` | **sim** | Senha do RabbitMQ | - |
| `RabbitMQ__Host` | não | Host do RabbitMQ | `localhost` |
| `RabbitMQ__Port` | não | Porta do RabbitMQ | `5672` |
| `MongoDB__ConnectionString` | não | Connection string do MongoDB (se contiver credenciais, trate como segredo) | `mongodb://localhost:27017` |
| `Redis__ConnectionString` | não | Endereço do Redis | `localhost:6379` |
| `Elasticsearch__Url` | não | URL do Elasticsearch (busca). Sem autenticação neste projeto | `http://localhost:9200` |
| `NEW_RELIC_LICENSE_KEY` | em produção | Chave de licença da New Relic | - |

Não há credenciais no código nem nos `appsettings.json`. Se uma variável obrigatória faltar, a aplicação não inicia e informa qual chave está faltando. Em produção (AKS) os valores vêm do Azure Key Vault, injetados como variáveis de ambiente.

## Como rodar localmente

### 1. Infraestrutura (SQL Server, RabbitMQ, MongoDB e Redis)

```bash
cp .env.example .env     # edite o .env com os seus valores (o .env não é versionado)
docker compose up -d
```

Este compose usa portas deslocadas para conviver com os outros: SQL Server **1434**, RabbitMQ **5673** (management em 15673), MongoDB **27018** e Redis **6380**. O MongoDB guarda as avaliações e o log de eventos; o Redis guarda o cache de jogos e bibliotecas.
**Elasticsearch (opcional, só para a busca).** Este compose não inclui o Elasticsearch. Sem ele a API sobe e funciona normalmente (CRUD, compra, biblioteca), e apenas `GET /api/jogos/search` responde `503`; o índice é reconstruído sozinho quando o Elasticsearch aparece. Para testar a busca, suba o do compose da orquestração (`docker compose up -d elasticsearch` em `FCG_Orquestracao`, porta 9200, que é a URL padrão) ou aponte `Elasticsearch:Url` para outro.

### 2. Segredos (dotnet user-secrets)

Os valores abaixo são **exemplos falsos**. Use o `SA_PASSWORD` e o usuário/senha do RabbitMQ que você definiu no `.env`. As chaves `RabbitMQ:Port`, `MongoDB:ConnectionString` e `Redis:ConnectionString` não são segredos, mas precisam apontar para as portas do compose.

API:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1434;Database=FCG_Catalogo;User Id=sa;Password=<SA_PASSWORD do .env>;TrustServerCertificate=True;" --project FCG.Catalogo.Presentation
dotnet user-secrets set "RabbitMQ:Port" "5673" --project FCG.Catalogo.Presentation
dotnet user-secrets set "MongoDB:ConnectionString" "mongodb://localhost:27018" --project FCG.Catalogo.Presentation
dotnet user-secrets set "Redis:ConnectionString" "localhost:6380" --project FCG.Catalogo.Presentation
dotnet user-secrets set "RabbitMQ:Usuario" "troque_este_usuario" --project FCG.Catalogo.Presentation
dotnet user-secrets set "RabbitMQ:Senha" "TroqueEstaSenha#2" --project FCG.Catalogo.Presentation
dotnet user-secrets set "JwtSettings:Segredo" "troque-esta-chave-com-no-minimo-32-caracteres" --project FCG.Catalogo.Presentation
```

Worker (não usa JWT):

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1434;Database=FCG_Catalogo;User Id=sa;Password=<SA_PASSWORD do .env>;TrustServerCertificate=True;" --project FCG.Catalogo.Worker
dotnet user-secrets set "RabbitMQ:Port" "5673" --project FCG.Catalogo.Worker
dotnet user-secrets set "MongoDB:ConnectionString" "mongodb://localhost:27018" --project FCG.Catalogo.Worker
dotnet user-secrets set "Redis:ConnectionString" "localhost:6380" --project FCG.Catalogo.Worker
dotnet user-secrets set "RabbitMQ:Usuario" "troque_este_usuario" --project FCG.Catalogo.Worker
dotnet user-secrets set "RabbitMQ:Senha" "TroqueEstaSenha#2" --project FCG.Catalogo.Worker
```

### 3. Executar

Ao iniciar, a API aplica as migrations do EF Core automaticamente (o banco `FCG_Catalogo` é criado se não existir).

```bash
dotnet run --project FCG.Catalogo.Presentation
```

> ⚠️ Este repositório não contém credenciais. Os exemplos acima usam valores falsos; nunca versione o `.env` nem os seus segredos reais.
