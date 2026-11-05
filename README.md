# FCG Catálogo

Microsserviço responsável pelo gerenciamento de jogos e fluxo de compra da plataforma FIAP Cloud Games.

## Responsabilidades

- CRUD de jogos
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
| `NEW_RELIC_LICENSE_KEY` | em produção | Chave de licença da New Relic | - |

Não há credenciais no código nem nos `appsettings.json`. Se uma variável obrigatória faltar, a aplicação não inicia e informa qual chave está faltando. Em produção (AKS) os valores vêm do Azure Key Vault, injetados como variáveis de ambiente.

## Como rodar localmente

### 1. Infraestrutura (SQL Server, RabbitMQ, MongoDB e Redis)

```bash
cp .env.example .env     # edite o .env com os seus valores (o .env não é versionado)
docker compose up -d
```

Este compose usa portas deslocadas para conviver com os outros: SQL Server **1434**, RabbitMQ **5673** (management em 15673), MongoDB **27018** e Redis **6380**. O MongoDB guarda as avaliações e o log de eventos; o Redis guarda o cache de jogos e bibliotecas.

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