using Microsoft.AspNetCore.Mvc;
using SecondBlock.Products.API.Controllers;
using SecondBlock.Products.Business.DTOs;
using SecondBlock.Products.Business.Services.Interfaces;

namespace SecondBlock.Products.API.UnitTests;

public class ProductsControllerTests
{
    private readonly StubProductService _service = new();
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _controller = new ProductsController(_service);
    }

    [Fact]
    public void GetAll_WithProducts_ReturnsOkWithProductDtos()
    {
        _service.Products = [CreateTestProductDto()];

        var response = Assert.IsType<OkObjectResult>(_controller.GetAll());

        Assert.Equal(200, response.StatusCode);
        Assert.Same(_service.Products, response.Value);
    }

    [Fact]
    public void GetAll_WithoutProducts_ReturnsOkWithEmptyList()
    {
        var response = Assert.IsType<OkObjectResult>(_controller.GetAll());

        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<ProductDto>>(response.Value));
    }

    [Fact]
    public void GetById_ExistingProduct_ReturnsOkWithDto()
    {
        _service.Product = CreateTestProductDto();

        var response = Assert.IsType<OkObjectResult>(_controller.GetById(7));

        Assert.Same(_service.Product, response.Value);
        Assert.Equal(7, _service.RequestedId);
    }

    [Fact]
    public void GetById_MissingProduct_ReturnsNotFound()
    {
        Assert.IsType<NotFoundResult>(_controller.GetById(99));
    }

    [Fact]
    public void Create_WithValidDto_ReturnsCreatedAtActionWithDtoAndGetRoute()
    {
        _service.Product = CreateTestProductDto();
        var input = new CreateProductDto { Name = "Keyboard", Price = 150, Category = "Hardware" };

        var response = Assert.IsType<CreatedAtActionResult>(_controller.Create(input));

        Assert.Equal(201, response.StatusCode);
        Assert.Equal(nameof(ProductsController.GetById), response.ActionName);
        Assert.Equal(7, response.RouteValues!["id"]);
        Assert.Same(_service.Product, response.Value);
        Assert.Same(input, _service.ReceivedDto);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Update_WithExistingOrMissingProduct_ReturnsNoContentOrNotFound(bool exists)
    {
        _service.OperationSucceeds = exists;
        var input = new CreateProductDto { Name = "Monitor", Price = 800, Category = "Displays" };

        var response = _controller.Update(7, input);

        if (exists)
        {
            Assert.IsType<NoContentResult>(response);
        }
        else
        {
            Assert.IsType<NotFoundResult>(response);
        }
        Assert.Equal(7, _service.UpdatedId);
        Assert.Same(input, _service.ReceivedDto);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Delete_WithExistingOrMissingProduct_ReturnsNoContentOrNotFound(bool exists)
    {
        _service.OperationSucceeds = exists;

        var response = _controller.Delete(7);

        if (exists)
        {
            Assert.IsType<NoContentResult>(response);
        }
        else
        {
            Assert.IsType<NotFoundResult>(response);
        }
        Assert.Equal(7, _service.DeletedId);
    }

    private static ProductDto CreateTestProductDto()
    {
        return new ProductDto
        {
            Id = 7, Name = "Keyboard", Price = 150, Category = "Hardware", IsActive = true
        };
    }

    private sealed class StubProductService : IProductService
    {
        public List<ProductDto> Products { get; set; } = [];
        public ProductDto? Product { get; set; }
        public CreateProductDto? ReceivedDto { get; private set; }
        public int? RequestedId { get; private set; }
        public int? UpdatedId { get; private set; }
        public int? DeletedId { get; private set; }
        public bool OperationSucceeds { get; set; }

        public List<ProductDto> GetAll()
        {
            return Products;
        }

        public ProductDto? GetById(int id)
        {
            RequestedId = id;
            return Product;
        }

        public ProductDto Create(CreateProductDto dto)
        {
            ReceivedDto = dto;
            return Product!;
        }

        public bool Update(int id, CreateProductDto dto)
        {
            UpdatedId = id;
            ReceivedDto = dto;
            return OperationSucceeds;
        }

        public bool Delete(int id)
        {
            DeletedId = id;
            return OperationSucceeds;
        }
    }
}
