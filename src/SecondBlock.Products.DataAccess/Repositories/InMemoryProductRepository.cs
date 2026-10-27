using SecondBlock.Products.DataAccess.Entities;
using SecondBlock.Products.DataAccess.Repositories.Interfaces;

namespace SecondBlock.Products.DataAccess.Repositories;

public class InMemoryProductRepository : IProductRepository
{
    private static readonly List<Product> s_products = new List<Product>();
    private static int s_nextId = 1;

    public List<Product> GetAll()
    {
        return s_products.ToList();
    }

    public Product? GetById(int id)
    {
        foreach (var product in s_products)
        {
            if (product.Id == id)
            {
                return product;
            }
        }

        return null;
    }

    public Product Create(Product product)
    {
        if (product == null)
        {
            throw new ArgumentNullException(nameof(product));
        }

        product.Id = s_nextId;
        s_nextId++;
        s_products.Add(product);

        return product;
    }

    public bool Update(Product product)
    {
        if (product == null)
        {
            throw new ArgumentNullException(nameof(product));
        }

        var existingProduct = GetById(product.Id);

        if (existingProduct == null)
        {
            return false;
        }

        existingProduct.Name = product.Name;
        existingProduct.Price = product.Price;
        existingProduct.Category = product.Category;
        existingProduct.IsActive = product.IsActive;

        return true;
    }

    public bool Delete(int id)
    {
        var product = GetById(id);

        if (product == null)
        {
            return false;
        }

        return s_products.Remove(product);
    }
}
