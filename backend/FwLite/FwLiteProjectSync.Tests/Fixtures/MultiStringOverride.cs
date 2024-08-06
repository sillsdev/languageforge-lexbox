using MiniLcm;
using Soenneker.Utils.AutoBogus.Context;
using Soenneker.Utils.AutoBogus.Override;

namespace FwLiteProjectSync.Tests.Fixtures;

public class MultiStringOverride: AutoFakerOverride<MultiString>
{
    public override void Generate(AutoFakerOverrideContext context)
    {
        var target = context.Instance as MultiString;
        if (target is null)
        {
            context.Instance = target = new MultiString();
        }
        var wordsArray = context.Faker.Random.WordsArray(1, 4);
        foreach (var word in wordsArray)
        {
            target[context.Faker.Random.String(2, 'a', 'z')] = word;
        }
    }
}
