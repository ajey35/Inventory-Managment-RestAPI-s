using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Inventory.Api.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Inventory.Tests;

public sealed class InventoryApiTests
{
    [Fact]
    public async Task ProductWorkflow_CreatesSearchesRetrievesAndAdjustsStock()
    {
        using var factory = new InventoryApiFactory();
        using var client = factory.CreateClient();

        var categoryResponse = await client.PostAsJsonAsync(
            "/api/categories",
            new CategoryRequest("Hardware", "Fasteners and tools"));
        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        Assert.NotNull(category);

        var createResponse = await client.PostAsJsonAsync(
            "/api/products",
            new CreateProductRequest("Steel bolt", "BOLT-001", 1.25m, 10, category.Id));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var product = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(product);
        Assert.Equal("BOLT-001", product.Sku);

        var listResponse = await client.GetAsync(
            "/api/products?search=bolt&page=1&pageSize=10&sortBy=name&sortOrder=asc");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResponse<ProductResponse>>();
        Assert.NotNull(page);
        Assert.Equal(1, page.TotalCount);
        Assert.Equal(product.Id, Assert.Single(page.Items).Id);

        var skuSearchResponse = await client.GetAsync("/api/products?search=BOLT-001");
        var skuSearch = await skuSearchResponse.Content.ReadFromJsonAsync<PagedResponse<ProductResponse>>();
        Assert.Equal(product.Id, Assert.Single(skuSearch!.Items).Id);

        var getResponse = await client.GetAsync($"/api/products/{product.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var adjustResponse = await client.PatchAsJsonAsync(
            $"/api/products/{product.Id}/stock",
            new AdjustStockRequest(-3));
        Assert.Equal(HttpStatusCode.OK, adjustResponse.StatusCode);
        var adjustedProduct = await adjustResponse.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.Equal(7, adjustedProduct!.StockQuantity);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/products/{product.Id}",
            new UpdateProductRequest("Hex bolt", "BOLT-002", 1.5m, 7, category.Id));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var deleteProductResponse = await client.DeleteAsync($"/api/products/{product.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteProductResponse.StatusCode);
        var deletedProductResponse = await client.GetAsync($"/api/products/{product.Id}");
        Assert.Equal(HttpStatusCode.NotFound, deletedProductResponse.StatusCode);

        var deleteCategoryResponse = await client.DeleteAsync($"/api/categories/{category.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteCategoryResponse.StatusCode);
    }

    [Fact]
    public async Task AdjustStock_ReturnsProblemDetailsWhenStockWouldBecomeNegative()
    {
        using var factory = new InventoryApiFactory();
        using var client = factory.CreateClient();
        var category = await CreateCategoryAsync(client);
        var productResponse = await client.PostAsJsonAsync(
            "/api/products",
            new CreateProductRequest("Washer", "WASH-1", 0.5m, 1, category.Id));
        var product = await productResponse.Content.ReadFromJsonAsync<ProductResponse>();

        var categoryDeleteResponse = await client.DeleteAsync($"/api/categories/{category.Id}");
        Assert.Equal(HttpStatusCode.Conflict, categoryDeleteResponse.StatusCode);

        var response = await client.PatchAsJsonAsync(
            $"/api/products/{product!.Id}/stock",
            new AdjustStockRequest(-2));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.StartsWith("application/problem+json", response.Content.Headers.ContentType?.ToString());
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Conflict", problem.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task InvalidPageSize_ReturnsBadRequestProblemDetails()
    {
        using var factory = new InventoryApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/products?pageSize=101");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Invalid request", problem.RootElement.GetProperty("title").GetString());
    }

    private static async Task<CategoryResponse> CreateCategoryAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/categories", new CategoryRequest("Hardware", null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryResponse>())!;
    }

    private sealed class InventoryApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
        }
    }
}
