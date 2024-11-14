using MiniLcm.Models;
using Soenneker.Utils.AutoBogus.Context;
using Soenneker.Utils.AutoBogus.Override;

namespace MiniLcm.Tests.AutoFakerHelpers;

public class MultiStringOverride(string[]? validWs = null): AutoFakerOverride<MultiString>
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
            var writingSystemId = validWs is not null
                ? context.Faker.Random.ArrayElement(validWs)
                : context.Faker.Random.String(2, 'a', 'z');
            target[writingSystemId] = word;
        }
    }
}
