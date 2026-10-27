using System.ComponentModel.DataAnnotations;

namespace SecondBlock.Products.Business.DTOs;

public class CreateProductDto
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must contain between {2} and {1} characters.")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Price is required.")]
    [Range(typeof(decimal), "0", "79228162514264337593543950335", MinimumIsExclusive = true, ParseLimitsInInvariantCulture = true, ErrorMessage = "Price must be greater than 0.")]
    public decimal? Price { get; set; }

    [Required(ErrorMessage = "Category is required.")]
    public string? Category { get; set; }
}