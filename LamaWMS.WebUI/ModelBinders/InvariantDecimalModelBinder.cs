using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LamaWMS.WebUI.ModelBinders;

public class InvariantDecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
            throw new ArgumentNullException(nameof(bindingContext));

        var valueResult =
            bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

        if (valueResult == ValueProviderResult.None)
            return Task.CompletedTask;

        bindingContext.ModelState.SetModelValue(
            bindingContext.ModelName,
            valueResult);

        var value = valueResult.FirstValue;

        if (string.IsNullOrWhiteSpace(value))
            return Task.CompletedTask;

        // Принимаем и запятую, и точку как десятичный разделитель.
        value = value.Trim().Replace(',', '.');

        if (decimal.TryParse(
            value,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var decimalValue))
        {
            bindingContext.Result =
                ModelBindingResult.Success(decimalValue);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName,
                "Недопустимое число.");
        }

        return Task.CompletedTask;
    }
}