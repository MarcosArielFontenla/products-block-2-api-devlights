using SecondBlock.Products.DataAccess.Entities;

namespace SecondBlock.Products.DataAccess.Repositories.Interfaces;

public interface IProductRepository
{
    List<Product> GetAll();
    Product? GetById(int id);
    Product Create(Product product);
    bool Update(Product product);
    bool Delete(int id);
}
