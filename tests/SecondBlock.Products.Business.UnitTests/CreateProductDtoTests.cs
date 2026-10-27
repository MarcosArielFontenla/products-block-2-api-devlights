using System.ComponentModel.DataAnnotations;
using System.Globalization;
using SecondBlock.Products.Business.DTOs;

namespace SecondBlock.Products.Business.UnitTests;

public class CreateProductDtoTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("AB")]
    public void Name_InvalidValue_ProducesFieldError(string? name)
    {
        var dto = CreateValidDto();
        dto.Name = name;

        Assert.Contains(Validate(dto), error => error.MemberNames.Contains(nameof(dto.Name)));
    }

    [Fact]
    public void Name_LongerThan100Characters_ProducesFieldError()
    {
        var dto = CreateValidDto();
        dto.Name = new string('A', 101);

        Assert.Contains(Validate(dto), error => error.MemberNames.Contains(nameof(dto.Name)));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(100)]
    public void Name_AtLengthBoundaries_IsValid(int length)
    {
        var dto = CreateValidDto();
        dto.Name = new string('A', length);

        Assert.Empty(Validate(dto));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("-0.01")]
    public void Price_MissingZeroOrNegative_ProducesFieldError(string? price)
    {
        var dto = CreateValidDto();
        dto.Price = price is null ? null : decimal.Parse(price, CultureInfo.InvariantCulture);

        Assert.Contains(Validate(dto), error => error.MemberNames.Contains(nameof(dto.Price)));
    }

    [Theory]
    [InlineData("0.0000000000000000000000000001")]
    [InlineData("0.001")]
    [InlineData("25.50")]
    [InlineData("79228162514264337593543950335")]
    public void Price_PositiveValue_IsValid(string price)
    {
        var dto = CreateValidDto();
        dto.Price = decimal.Parse(price, CultureInfo.InvariantCulture);

        Assert.Empty(Validate(dto));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Category_MissingOrBlank_ProducesFieldError(string? category)
    {
        var dto = CreateValidDto();
        dto.Category = category;

        Assert.Contains(Validate(dto), error => error.MemberNames.Contains(nameof(dto.Category)));
    }

    [Fact]
    public void Validate_WithArgentineCulture_AcceptsPositiveDecimalPrice()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("es-AR");
            Assert.Empty(Validate(CreateValidDto()));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    private static CreateProductDto CreateValidDto()
    {
        return new CreateProductDto
        {
            Name = "Keyboard", Price = 150.50m, Category = "Hardware"
        };
    }

    private static List<ValidationResult> Validate(CreateProductDto dto)
    {
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), errors, validateAllProperties: true);
        return errors;
    }
}
