using Inventory.Api.Api;
using Inventory.Api.Services;
using Inventory.Api.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IProductStore, InMemoryProductStore>();
builder.Services.AddSingleton<ICategoryStore, InMemoryCategoryStore>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapProductsEndpoints();
app.MapCategoriesEndpoints();

app.Run();

public partial class Program { }
