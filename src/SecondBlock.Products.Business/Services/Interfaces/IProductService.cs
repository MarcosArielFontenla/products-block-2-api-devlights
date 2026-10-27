using SecondBlock.Products.Business.DTOs;

namespace SecondBlock.Products.Business.Services.Interfaces;

public interface IProductService
{
    List<ProductDto> GetAll();
    ProductDto? GetById(int id);
    ProductDto Create(CreateProductDto dto);
    bool Update(int id, CreateProductDto dto);
    bool Delete(int id);
}
