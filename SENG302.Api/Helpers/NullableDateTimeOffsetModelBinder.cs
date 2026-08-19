using Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// Written by ChatGPT 5.6 Luna
/// </summary>

public class NullableDateTimeOffsetModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var value = bindingContext.ValueProvider
            .GetValue(bindingContext.ModelName);

        if (value == ValueProviderResult.None ||
            string.IsNullOrWhiteSpace(value.FirstValue))
        {
            bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        // Dumb hacky fix
        if (value.FirstValue.StartsWith("0000"))
        {
            bindingContext.Result = ModelBindingResult.Success(DateTimeOffset.MinValue);
        }
        else if (DateTimeOffset.TryParse(
            value.FirstValue,
            out var dateTimeOffset))
        {
            bindingContext.Result =
                ModelBindingResult.Success(dateTimeOffset);
        }
        else
        {
            // Invalid value → null, without a ModelState error
            bindingContext.Result = ModelBindingResult.Success(null);
        }

        return Task.CompletedTask;
    }
}
