using Inventory.Api.DTOs;
using Inventory.Api.Services;

namespace Inventory.Api.Api;

public static class ProductsEndpoints
{
    public static IEndpointRouteBuilder MapProductsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/products")
            .WithTags("Products");

        group.MapGet("/", async (
                IProductService productService,
                CancellationToken cancellationToken,
                string? search = null,
                Guid? categoryId = null,
                decimal? minPrice = null,
                decimal? maxPrice = null,
                string sortBy = "name",
                string sortOrder = "asc",
                int page = 1,
                int pageSize = 20) =>
            Results.Ok(await productService.GetAllAsync(new ProductListQuery
            {
                Search = search,
                CategoryId = categoryId,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SortBy = sortBy,
                SortOrder = sortOrder,
                Page = page,
                PageSize = pageSize
            }, cancellationToken)))
            .WithName("GetProducts")
            .WithSummary("Search, filter, sort, and paginate products")
            .Produces<PagedResponse<ProductResponse>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", async (
                Guid id,
                IProductService productService,
                CancellationToken cancellationToken) =>
            Results.Ok(await productService.GetByIdAsync(id, cancellationToken)))
            .WithName("GetProductById")
            .WithSummary("Get a product by ID")
            .Produces<ProductResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", async (
                CreateProductRequest request,
                IProductService productService,
                CancellationToken cancellationToken) =>
        {
            var product = await productService.CreateAsync(request, cancellationToken);
            return Results.CreatedAtRoute("GetProductById", new { product.Id }, product);
        })
            .WithName("CreateProduct")
            .WithSummary("Create a product")
            .Produces<ProductResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateProductRequest request,
                IProductService productService,
                CancellationToken cancellationToken) =>
            Results.Ok(await productService.UpdateAsync(id, request, cancellationToken)))
            .WithName("UpdateProduct")
            .WithSummary("Replace a product")
            .Produces<ProductResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", async (
                Guid id,
                IProductService productService,
                CancellationToken cancellationToken) =>
        {
            await productService.DeleteAsync(id, cancellationToken);
            return Results.NoContent();
        })
            .WithName("DeleteProduct")
            .WithSummary("Delete a product")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:guid}/stock", async (
                Guid id,
                AdjustStockRequest request,
                IProductService productService,
                CancellationToken cancellationToken) =>
            Results.Ok(await productService.AdjustStockAsync(id, request, cancellationToken)))
            .WithName("AdjustProductStock")
            .WithSummary("Adjust stock by a positive or negative quantity")
            .Produces<ProductResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }
}
