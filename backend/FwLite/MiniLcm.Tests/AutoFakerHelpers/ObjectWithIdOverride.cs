using MiniLcm.Models;
using Soenneker.Utils.AutoBogus.Context;
using Soenneker.Utils.AutoBogus.Generators;

namespace MiniLcm.Tests.AutoFakerHelpers;

public class ObjectWithIdOverride : AutoFakerGeneratorOverride
{
    public override bool CanOverride(AutoFakerContext context)
    {
        return context.GenerateType.IsAssignableTo(typeof(IObjectWithId));
    }

    public override void Generate(AutoFakerOverrideContext context)
    {
        if (context.Instance is IObjectWithId obj)
        {
            obj.DeletedAt = null;
        }
    }
}
