using SecondBlock.Products.Business.Services;
using SecondBlock.Products.Business.Services.Interfaces;
using SecondBlock.Products.DataAccess.Repositories;
using SecondBlock.Products.DataAccess.Repositories.Interfaces;
using Swashbuckle.AspNetCore.SwaggerUI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductRepository, InMemoryProductRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    var swaggerUiOptions = new SwaggerUIOptions
    {
        DocumentTitle = "Products API - Swagger UI"
    };
    swaggerUiOptions.SwaggerEndpoint("/openapi/v1.json", "Products API v1");
    app.UseSwaggerUI(swaggerUiOptions);
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
