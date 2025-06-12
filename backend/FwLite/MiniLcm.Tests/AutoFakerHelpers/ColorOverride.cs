using System.Drawing;
using Soenneker.Utils.AutoBogus.Context;
using Soenneker.Utils.AutoBogus.Override;

namespace MiniLcm.Tests.AutoFakerHelpers;

public class ColorOverride : AutoFakerOverride<Color?>
{
    private static readonly Color[] ValidColors = [
        Color.Aqua,
        Color.Brown,
        Color.Red,
        Color.Blue,
        Color.GreenYellow,
        Color.Transparent
    ];
    public override bool Preinitialize => false;

    public override void Generate(AutoFakerOverrideContext context)
    {
        var namedColor = context.Faker.PickRandom(ValidColors);
        //if we use the named colors then they won't be treated as RGB codes but instead named colors
        var color = Color.FromArgb(namedColor.ToArgb());
        context.Instance = color;
    }
}
