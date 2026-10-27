using System.ComponentModel.DataAnnotations;
using SecondBlock.Products.Business.DTOs;
using SecondBlock.Products.Business.Services;
using SecondBlock.Products.DataAccess.Entities;
using SecondBlock.Products.DataAccess.Repositories.Interfaces;

namespace SecondBlock.Products.Business.UnitTests;

public class ProductServiceTests
{
    private readonly StubProductRepository _repository = new();
    private readonly ProductService _service;

    public ProductServiceTests() => _service = new ProductService(_repository);

    [Fact]
    public void GetAll_MapsEveryProductIncludingItsActiveState()
    {
        _repository.Products = [CreateTestProduct(), new Product
        {
            Id = 9, Name = "Mouse", Price = 25.50m, Category = "Accessories", IsActive = true
        }];

        var result = _service.GetAll();

        Assert.Collection(result,
            first => AssertExpectedProduct(first),
            second => Assert.Equal((9, "Mouse", 25.50m, "Accessories", true),
                (second.Id, second.Name, second.Price, second.Category, second.IsActive)));
    }

    [Fact]
    public void GetAll_WithoutProducts_ReturnsEmptyList()
    {
        Assert.Empty(_service.GetAll());
    }

    [Fact]
    public void GetById_ExistingProduct_ReturnsDtoIndependentOfEntity()
    {
        _repository.ProductToFind = CreateTestProduct();

        var result = Assert.IsType<ProductDto>(_service.GetById(7));

        AssertExpectedProduct(result);
        result.Name = "DTO change";
        Assert.Equal("Keyboard", _repository.ProductToFind.Name);
        Assert.Equal(7, _repository.RequestedId);
    }

    [Fact]
    public void GetById_MissingProduct_ReturnsNull()
    {
        Assert.Null(_service.GetById(99));
    }

    [Fact]
    public void Create_SavesActiveProductAndReturnsRepositoryAssignedId()
    {
        var dto = CreateValidDto();

        var result = _service.Create(dto);

        var storedProduct = Assert.IsType<Product>(_repository.CreatedProduct);
        Assert.Equal(dto.Name, storedProduct.Name);
        Assert.Equal(dto.Price, storedProduct.Price);
        Assert.Equal(dto.Category, storedProduct.Category);
        Assert.True(storedProduct.IsActive);
        Assert.Equal(42, result.Id);
        Assert.Equal((42, "Monitor", 800m, "Displays", true),
            (result.Id, result.Name, result.Price, result.Category, result.IsActive));
    }

    [Fact]
    public void Create_InvalidData_DoesNotSaveProduct()
    {
        var dto = CreateValidDto();
        dto.Price = 0;

        Assert.Throws<ValidationException>(() => _service.Create(dto));
        Assert.Null(_repository.CreatedProduct);
    }

    [Fact]
    public void Update_PreservesIdAndOriginalActiveState()
    {
        _repository.ProductToFind = CreateTestProduct();

        var wasUpdated = _service.Update(7, CreateValidDto());

        Assert.True(wasUpdated);
        var storedProduct = Assert.IsType<Product>(_repository.UpdatedProduct);
        Assert.Equal((7, "Monitor", 800m, "Displays", false),
            (storedProduct.Id, storedProduct.Name, storedProduct.Price, storedProduct.Category, storedProduct.IsActive));
    }

    [Fact]
    public void Update_MissingProduct_DoesNotSave()
    {
        Assert.False(_service.Update(99, CreateValidDto()));
        Assert.Null(_repository.UpdatedProduct);
    }

    [Fact]
    public void Update_ProductDeletedBeforeSave_ReturnsFalse()
    {
        _repository.ProductToFind = CreateTestProduct();
        _repository.UpdateSucceeds = false;

        Assert.False(_service.Update(7, CreateValidDto()));
    }

    [Fact]
    public void Update_InvalidData_DoesNotSaveChanges()
    {
        _repository.ProductToFind = CreateTestProduct();
        var dto = CreateValidDto();
        dto.Name = "AB";

        Assert.Throws<ValidationException>(() => _service.Update(7, dto));
        Assert.Null(_repository.UpdatedProduct);
        Assert.Equal("Keyboard", _repository.ProductToFind.Name);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Delete_ReturnsRepositoryResult(bool exists)
    {
        _repository.DeleteSucceeds = exists;

        Assert.Equal(exists, _service.Delete(7));
        Assert.Equal(7, _repository.DeletedId);
    }

    private static CreateProductDto CreateValidDto() => new()
    {
        Name = "Monitor", Price = 800, Category = "Displays"
    };

    private static Product CreateTestProduct() => new()
    {
        Id = 7, Name = "Keyboard", Price = 150, Category = "Hardware", IsActive = false
    };

    private static void AssertExpectedProduct(ProductDto dto) =>
        Assert.Equal((7, "Keyboard", 150m, "Hardware", false),
            (dto.Id, dto.Name, dto.Price, dto.Category, dto.IsActive));

    private sealed class StubProductRepository : IProductRepository
    {
        public List<Product> Products { get; set; } = [];
        public Product? ProductToFind { get; set; }
        public Product? CreatedProduct { get; private set; }
        public Product? UpdatedProduct { get; private set; }
        public int? RequestedId { get; private set; }
        public int? DeletedId { get; private set; }
        public bool UpdateSucceeds { get; set; } = true;
        public bool DeleteSucceeds { get; set; }

        public List<Product> GetAll() => Products;

        public Product? GetById(int id)
        {
            RequestedId = id;
            return ProductToFind;
        }

        public Product Create(Product product)
        {
            CreatedProduct = product;
            product.Id = 42;
            return product;
        }

        public bool Update(Product product)
        {
            UpdatedProduct = product;
            return UpdateSucceeds;
        }

        public bool Delete(int id)
        {
            DeletedId = id;
            return DeleteSucceeds;
        }
    }
}
