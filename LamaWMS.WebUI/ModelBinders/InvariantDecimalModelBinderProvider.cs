using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LamaWMS.WebUI.ModelBinders;

public class InvariantDecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(
        ModelBinderProviderContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        if (context.Metadata.ModelType == typeof(decimal) ||
            context.Metadata.ModelType == typeof(decimal?))
        {
            return new InvariantDecimalModelBinder();
        }

        return null;
    }
}