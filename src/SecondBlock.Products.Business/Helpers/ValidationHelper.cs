using System.ComponentModel.DataAnnotations;

namespace SecondBlock.Products.Business.Helpers;

public static class ValidationHelper
{
    public static void Validate(object instance)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        var validationContext = new ValidationContext(instance);
        Validator.ValidateObject(instance, validationContext, validateAllProperties: true);
    }
}