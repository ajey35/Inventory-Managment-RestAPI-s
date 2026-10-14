using Inventory.Api.DTOs;
using Inventory.Api.Services;

namespace Inventory.Api.Api;

public static class CategoriesEndpoints
{
    public static IEndpointRouteBuilder MapCategoriesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/categories")
            .WithTags("Categories");

        group.MapGet("/", async (
                ICategoryService categoryService,
                CancellationToken cancellationToken) =>
            Results.Ok(await categoryService.GetAllAsync(cancellationToken)))
            .WithName("GetCategories")
            .WithSummary("Get all categories")
            .Produces<IReadOnlyList<CategoryResponse>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", async (
                Guid id,
                ICategoryService categoryService,
                CancellationToken cancellationToken) =>
            Results.Ok(await categoryService.GetByIdAsync(id, cancellationToken)))
            .WithName("GetCategoryById")
            .WithSummary("Get a category by ID")
            .Produces<CategoryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", async (
                CategoryRequest request,
                ICategoryService categoryService,
                CancellationToken cancellationToken) =>
        {
            var category = await categoryService.CreateAsync(request, cancellationToken);
            return Results.CreatedAtRoute("GetCategoryById", new { category.Id }, category);
        })
            .WithName("CreateCategory")
            .WithSummary("Create a category")
            .Produces<CategoryResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", async (
                Guid id,
                CategoryRequest request,
                ICategoryService categoryService,
                CancellationToken cancellationToken) =>
            Results.Ok(await categoryService.UpdateAsync(id, request, cancellationToken)))
            .WithName("UpdateCategory")
            .WithSummary("Replace a category")
            .Produces<CategoryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", async (
                Guid id,
                ICategoryService categoryService,
                CancellationToken cancellationToken) =>
        {
            await categoryService.DeleteAsync(id, cancellationToken);
            return Results.NoContent();
        })
            .WithName("DeleteCategory")
            .WithSummary("Delete a category with no products")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }
}
