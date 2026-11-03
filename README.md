# Cart Service

Shopping cart Web API (.NET 10, PostgreSQL, Redis, RabbitMQ).

## Requirements

- Docker Desktop
- .NET 10 SDK, only to run the tests or the API outside Docker

## Run

```bash
git clone cart-service.bundle cart-service
cd cart-service
docker compose up
```

With access to the GitHub repository, clone with `git clone https://github.com/josipdom/cart-service.git` instead.

Open Swagger at http://localhost:8080/swagger.

To also start Elasticsearch and Kibana (http://localhost:5601) for logs:

```bash
docker compose --profile observability up
```

To stop everything:

```bash
docker compose --profile observability down
```

To run the API outside Docker (from Visual Studio or the command line), start only the infrastructure:

```bash
docker compose up -d postgres redis rabbitmq
dotnet run --project src/Cart.Api
```

Swagger is then at http://localhost:5205/swagger.

## Try the API

1. In Swagger, call `POST /api/dev/token` with `{ "userId": "test-user" }`.
2. Copy `accessToken`, click Authorize and paste it.
3. Call the cart endpoints. Test products are `SKU-1001` to `SKU-1005`.

## Tests

```bash
dotnet test
```

Docker must be running, because the integration tests start their own containers.
