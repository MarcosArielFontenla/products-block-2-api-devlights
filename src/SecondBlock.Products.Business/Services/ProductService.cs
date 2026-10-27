using SecondBlock.Products.Business.DTOs;
using SecondBlock.Products.Business.Helpers;
using SecondBlock.Products.Business.Mappers;
using SecondBlock.Products.Business.Services.Interfaces;
using SecondBlock.Products.DataAccess.Repositories.Interfaces;

namespace SecondBlock.Products.Business.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public List<ProductDto> GetAll()
    {
        var products = _productRepository.GetAll();
        var productDtos = new List<ProductDto>();

        foreach (var product in products)
        {
            var productDto = ProductMapper.ToDto(product);
            productDtos.Add(productDto);
        }

        return productDtos;
    }

    public ProductDto? GetById(int id)
    {
        var product = _productRepository.GetById(id);

        if (product == null)
        {
            return null;
        }

        var productDto = ProductMapper.ToDto(product);
        return productDto;
    }

    public ProductDto Create(CreateProductDto dto)
    {
        ValidationHelper.Validate(dto);

        var product = ProductMapper.ToEntity(dto);
        var createdProduct = _productRepository.Create(product);
        var productDto = ProductMapper.ToDto(createdProduct);

        return productDto;
    }

    public bool Update(int id, CreateProductDto dto)
    {
        ValidationHelper.Validate(dto);

        var product = _productRepository.GetById(id);
        if (product == null)
        {
            return false;
        }

        ProductMapper.UpdateEntity(dto, product);

        return _productRepository.Update(product);
    }

    public bool Delete(int id)
    {
        return _productRepository.Delete(id);
    }
}
