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

## Variáveis de ambiente

| Variável | Descrição | Padrão |
|----------|-----------|--------|
| `ConnectionStrings__DefaultConnection` | Connection string do SQL Server | - |
| `RabbitMQ__Host` | Host do RabbitMQ | `localhost` |
| `RabbitMQ__Port` | Porta do RabbitMQ | `5672` |
| `RabbitMQ__Usuario` | Usuário do RabbitMQ | `guest` |
| `RabbitMQ__Senha` | Senha do RabbitMQ | `guest` |

## Como rodar localmente

```bash
dotnet run --project FCG.Catalogo.Presentation
```