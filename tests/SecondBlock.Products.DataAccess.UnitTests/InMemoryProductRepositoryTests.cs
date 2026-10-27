using SecondBlock.Products.DataAccess.Entities;
using SecondBlock.Products.DataAccess.Repositories;

namespace SecondBlock.Products.DataAccess.UnitTests;

public class InMemoryProductRepositoryTests : IDisposable
{
    private readonly InMemoryProductRepository _repository;
    private readonly List<int> _createdIds = new List<int>();

    public InMemoryProductRepositoryTests()
    {
        _repository = new InMemoryProductRepository();
    }

    [Fact]
    public void Create_WithProduct_AssignsIdAndStoresData()
    {
        var input = new Product { Id = -1, Name = "Keyboard", Price = 150, Category = "Hardware" };

        var createdProduct = CreateAndTrackProduct(input);

        Assert.True(createdProduct.Id > 0);
        Assert.Equal(createdProduct.Id, input.Id);
        var storedProduct = Assert.IsType<Product>(_repository.GetById(createdProduct.Id));
        Assert.Equal("Keyboard", storedProduct.Name);
        Assert.Equal(150m, storedProduct.Price);
    }

    [Fact]
    public void GetById_MissingProduct_ReturnsNull()
    {
        Assert.Null(_repository.GetById(-1));
    }

    [Fact]
    public void GetById_WithExistingProduct_ReturnsMatchingProduct()
    {
        CreateAndTrackProduct();
        var createdProduct = CreateAndTrackProduct(new Product
        {
            Name = "Mouse", Price = 25, Category = "Accessories", IsActive = false
        });
        var retrievedProduct = Assert.IsType<Product>(_repository.GetById(createdProduct.Id));

        Assert.Equal(createdProduct.Id, retrievedProduct.Id);
        Assert.Equal("Mouse", retrievedProduct.Name);
        Assert.Equal(25m, retrievedProduct.Price);
        Assert.Equal("Accessories", retrievedProduct.Category);
        Assert.False(retrievedProduct.IsActive);
    }

    [Fact]
    public void GetAll_WithProducts_ReturnsSeparateList()
    {
        var first = CreateAndTrackProduct();
        var products = _repository.GetAll();
        var second = CreateAndTrackProduct();

        Assert.DoesNotContain(products, product => product.Id == second.Id);
        Assert.Contains(products, product => product.Id == first.Id);
        products.Clear();
        Assert.NotNull(_repository.GetById(first.Id));
        Assert.NotNull(_repository.GetById(second.Id));
    }

    [Fact]
    public void GetById_FromAnotherRepositoryInstance_ReturnsSharedProduct()
    {
        var createdProduct = CreateAndTrackProduct();
        var otherRepository = new InMemoryProductRepository();

        var foundProduct = otherRepository.GetById(createdProduct.Id);

        Assert.NotNull(foundProduct);
        Assert.Equal(createdProduct.Name, foundProduct.Name);
    }

    [Fact]
    public void Update_WithExistingProduct_UpdatesStoredData()
    {
        var createdProduct = CreateAndTrackProduct();
        var changes = new Product
        {
            Id = createdProduct.Id, Name = "Monitor", Price = 800, Category = "Displays", IsActive = false
        };

        var wasUpdated = _repository.Update(changes);

        Assert.True(wasUpdated);
        var storedProduct = Assert.IsType<Product>(_repository.GetById(createdProduct.Id));
        Assert.Equal("Monitor", storedProduct.Name);
        Assert.Equal(800m, storedProduct.Price);
        Assert.Equal("Displays", storedProduct.Category);
        Assert.False(storedProduct.IsActive);
    }

    [Fact]
    public void Update_MissingProduct_ReturnsFalse()
    {
        Assert.False(_repository.Update(new Product { Id = -1 }));
    }

    [Fact]
    public void Delete_WithExistingProduct_RemovesProductAndReturnsFalseWhenRepeated()
    {
        var createdProduct = CreateAndTrackProduct();

        Assert.True(_repository.Delete(createdProduct.Id));
        Assert.Null(_repository.GetById(createdProduct.Id));
        Assert.DoesNotContain(_repository.GetAll(), product => product.Id == createdProduct.Id);
        Assert.False(_repository.Delete(createdProduct.Id));
    }

    [Fact]
    public void Create_AfterDeletion_DoesNotReuseId()
    {
        var first = CreateAndTrackProduct();
        _repository.Delete(first.Id);

        var second = CreateAndTrackProduct();

        Assert.True(second.Id > first.Id);
    }

    private Product CreateAndTrackProduct(Product? product = null)
    {
        var createdProduct = _repository.Create(product ?? new Product
        {
            Name = "Keyboard", Price = 150, Category = "Hardware", IsActive = true
        });
        _createdIds.Add(createdProduct.Id);
        return createdProduct;
    }

    public void Dispose()
    {
        foreach (var id in _createdIds)
        {
            _repository.Delete(id);
        }
    }
}
