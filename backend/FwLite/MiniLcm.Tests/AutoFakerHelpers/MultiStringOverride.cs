using Soenneker.Utils.AutoBogus.Context;
using Soenneker.Utils.AutoBogus.Override;

namespace MiniLcm.Tests.AutoFakerHelpers;

public class MultiStringOverride(string[]? validWs = null): AutoFakerOverride<MultiString>
{
    public override bool Preinitialize => false;
    public override void Generate(AutoFakerOverrideContext context)
    {
        if (context.Instance is not MultiString target)
        {
            context.Instance = target = [];
        }
        var validWritingSystems = validWs ?? WritingSystemCodes.ValidTwoLetterCodes;
        if (validWritingSystems.Length == 0) throw new ArgumentException("validWs cannot be an empty array");

        var count = context.Faker.Random.Int(1, Math.Min(4, validWritingSystems.Length));
        var writingSystems = context.Faker.Random.ListItems(validWritingSystems, count);
        foreach (var writingSystemId in writingSystems)
        {
            var word = context.Faker.Random.Word();
            target[writingSystemId] = word;
        }
    }
}

public class RichMultiStringOverride(string[]? validWs = null): AutoFakerOverride<RichMultiString>
{
    public override bool Preinitialize => false;

    public override void Generate(AutoFakerOverrideContext context)
    {
        if (context.Instance is not RichMultiString target)
        {
            context.Instance = target = [];
        }

        var validWritingSystems = validWs ?? WritingSystemCodes.ValidTwoLetterCodes;
        if (validWritingSystems.Length == 0) throw new ArgumentException("validWs cannot be an empty array");

        var count = context.Faker.Random.Int(1, Math.Min(4, validWritingSystems.Length));
        var writingSystems = context.Faker.Random.ListItems(validWritingSystems, count);
        foreach (var writingSystemId in writingSystems)
        {
            var wordsArray = context.Faker.Random.WordsArray(1, 4);
            var spans = new List<RichSpan>();
            foreach (var word in wordsArray)
            {
                if (context.Faker.Random.Bool())
                {
                    spans.Add(new()
                    {
                        Text = word,
                        Ws = writingSystemId,
                        //ensure that 2 spans don't ever have the same props meaning they get merged by LCM
                        Tags = [Guid.NewGuid()]
                    });
                }
                else
                {
                    spans.Add(context.AutoFaker.Generate<RichSpan>());
                }
            }

            target[writingSystemId] = new RichString([..spans]);
        }
    }
}

public class RichSpanOverride(bool minimal = false) : AutoFakerOverride<RichSpan>
{
    public override bool Preinitialize => true;

    public override void Generate(AutoFakerOverrideContext context)
    {
        if (!minimal || context.Instance is not RichSpan existing) return;

        // A much less noisy representation that covers all the data types
        var minimalSpan = new RichSpan
        {
            Text = existing.Text,
            Ws = existing.Ws,
            Tags = existing.Tags,
            Bold = existing.Bold,
            FontSize = existing.FontSize,
            ForeColor = existing.ForeColor,
            ObjData = existing.ObjData,
        };

        context.Instance = minimalSpan;
    }
}
