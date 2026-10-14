# Inventory Management API

A modular, beginner-friendly REST API built with C# and ASP.NET Core on .NET 10. Storage is in-memory: all data is lost when the application stops.

## Run the API

From the solution directory:

```bash
dotnet run --project Inventory.Api
```

Use the URL printed by ASP.NET Core. In the Development environment, Swagger UI is available at `/swagger`.

```bash
curl -i http://localhost:5014/api/products
```

The initial response is `200 OK` with an empty paged result. The exact port may differ if the launch profile or `ASPNETCORE_URLS` configuration is changed.

## API endpoints

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/api/products` | Search, filter, sort, and paginate products |
| `GET` | `/api/products/{id}` | Retrieve one product |
| `POST` | `/api/products` | Create a product |
| `PUT` | `/api/products/{id}` | Replace a product |
| `DELETE` | `/api/products/{id}` | Delete a product |
| `PATCH` | `/api/products/{id}/stock` | Adjust stock by a signed quantity |
| `GET` | `/api/categories` | List categories |
| `GET` | `/api/categories/{id}` | Retrieve one category |
| `POST` | `/api/categories` | Create a category |
| `PUT` | `/api/categories/{id}` | Replace a category |
| `DELETE` | `/api/categories/{id}` | Delete a category with no assigned products |

Product list query parameters:

- `search`: case-insensitive match against product name or SKU.
- `categoryId`, `minPrice`, `maxPrice`: optional filters.
- `sortBy`: `name`, `sku`, `price`, or `stock`; `sortOrder`: `asc` or `desc`.
- `page`: starts at 1; `pageSize`: 1 through 100 (defaults to 20).

Example:

```bash
curl -i 'http://localhost:5014/api/products?search=bolt&sortBy=price&sortOrder=asc&page=1&pageSize=10'
```

Create a category first, then use its returned `id` as the product's `categoryId`:

```bash
curl -i -X POST http://localhost:5014/api/categories \
  -H 'Content-Type: application/json' \
  -d '{"name":"Hardware","description":"Fasteners and tools"}'

curl -i -X POST http://localhost:5014/api/products \
  -H 'Content-Type: application/json' \
  -d '{"name":"Steel bolt","sku":"BOLT-001","price":1.25,"stockQuantity":10,"categoryId":"<category-id>"}'

curl -i -X PATCH 'http://localhost:5014/api/products/<product-id>/stock' \
  -H 'Content-Type: application/json' \
  -d '{"adjustment":-2}'
```

Stock cannot become negative. Product SKUs and category names must be unique (case-insensitive). A category with assigned products cannot be deleted.

## HTTP behavior

- `200 OK`: successful read, update, or stock adjustment.
- `201 Created`: successful create; the response includes a `Location` header.
- `204 No Content`: successful delete.
- `400 Bad Request`: invalid values or query parameters.
- `404 Not Found`: requested product or category does not exist.
- `409 Conflict`: duplicate SKU/name, insufficient stock, or a category is still in use.
- `500 Internal Server Error`: unexpected server errors; details are not exposed to clients.

Errors use `application/problem+json` (ProblemDetails) and include a trace ID. Request validation and expected conflicts are handled by the application services; centralized exception middleware formats the HTTP error response and logs failures.

## Architecture and ASP.NET Core concepts

- `Inventory.Api/Program.cs` configures the host, dependency injection, Swagger, exception handling, and route groups.
- `Inventory.Api/Api` maps HTTP methods and URLs to application services. Minimal API route handlers serve the same role as Express routes or Spring MVC controllers.
- `Inventory.Api/Models` contains domain entities; `Inventory.Api/DTOs` defines the public JSON request and response shapes.
- `Inventory.Api/Services` contains business rules such as input validation, SKU uniqueness, and category references.
- `Inventory.Api/Storage` contains storage interfaces and thread-safe in-memory implementations.
- `Inventory.Tests` contains direct storage unit tests and HTTP-level integration tests using ASP.NET Core's test server.

An HTTP request moves through middleware (including centralized exception handling), endpoint routing, model binding, the service resolved through dependency injection, and storage. ASP.NET Core serializes the DTO returned by the endpoint into the HTTP response. `async`/`await` keeps the storage and service contracts ready for asynchronous I/O; `CancellationToken` propagates client/request cancellation.

Like Spring Boot, ASP.NET Core uses dependency injection and middleware/interceptors, but endpoint mapping and the host pipeline are configured in C# rather than Spring annotations and auto-configuration. Configuration uses ASP.NET Core's standard providers (JSON settings, environment variables, and command-line arguments); the default logging providers are available through injected `ILogger<T>`.

## Build and test

```bash
dotnet build InventoryManagement.slnx
dotnet test InventoryManagement.slnx
```
