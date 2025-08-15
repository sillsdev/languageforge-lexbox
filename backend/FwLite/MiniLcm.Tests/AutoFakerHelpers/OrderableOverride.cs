using Soenneker.Utils.AutoBogus.Context;
using Soenneker.Utils.AutoBogus.Generators;

namespace MiniLcm.Tests.AutoFakerHelpers;

public class OrderableOverride : AutoFakerGeneratorOverride
{
    public override bool CanOverride(AutoFakerContext context)
    {
        return context.GenerateType.IsAssignableTo(typeof(IOrderable));
    }

    public override void Generate(AutoFakerOverrideContext context)
    {
        if (context.Instance is IOrderable obj)
        {
            // Order should never be predefined
            obj.Order = 0;
        }
    }
}
