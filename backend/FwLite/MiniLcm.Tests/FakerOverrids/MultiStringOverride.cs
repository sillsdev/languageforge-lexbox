using MiniLcm.Models;
using Soenneker.Utils.AutoBogus.Context;
using Soenneker.Utils.AutoBogus.Override;

namespace MiniLcm.Tests.FakerOverrids;

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
            var ws = context.Faker.Random.ArrayElement(WritingSystemCodes.ValidTwoLetterCodes);
            target[ws] = word;
        }
    }
}
