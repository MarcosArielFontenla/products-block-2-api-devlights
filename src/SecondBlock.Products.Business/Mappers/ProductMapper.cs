using SecondBlock.Products.Business.DTOs;
using SecondBlock.Products.DataAccess.Entities;

namespace SecondBlock.Products.Business.Mappers;

public static class ProductMapper
{
    public static ProductDto ToDto(Product product)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        var productDto = new ProductDto();
        productDto.Id = product.Id;
        productDto.Name = product.Name;
        productDto.Price = product.Price;
        productDto.Category = product.Category;
        productDto.IsActive = product.IsActive;

        return productDto;
    }

    public static Product ToEntity(CreateProductDto dto)
    {
        var product = new Product();
        UpdateEntity(dto, product);
        return product;
    }

    public static void UpdateEntity(CreateProductDto dto, Product product)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        if (product == null)
            throw new ArgumentNullException(nameof(product));

        if (dto.Name == null || dto.Price == null || dto.Category == null)
            throw new ArgumentException("Product data must be validated before mapping.", nameof(dto));

        product.Name = dto.Name;
        product.Price = dto.Price.Value;
        product.Category = dto.Category;
    }
}