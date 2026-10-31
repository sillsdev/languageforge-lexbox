using System.Text.Json;

namespace MiniLcm.Tests.Models;

public class ComplexFormComponentTests
{
    private static ComplexFormComponent BankComponent()
    {
        var complexForm = new Entry { Id = Guid.NewGuid(), LexemeForm = { { "en", "bank account" } }, HomographNumber = 0 };
        var component = new Entry { Id = Guid.NewGuid(), LexemeForm = { { "en", "bank" } }, HomographNumber = 2 };
        return ComplexFormComponent.FromEntries(complexForm, component);
    }

    [Fact]
    public void FromEntries_TakesTheEntriesHomographNumbers()
    {
        var component = BankComponent();
        component.ComplexFormHomographNumber.Should().Be(0);
        component.ComponentHomographNumber.Should().Be(2);
    }

    // the global equivalency config excludes these members, so check Copy directly
    [Fact]
    public void Copy_CopiesHomographNumbers()
    {
        var component = BankComponent();
        component.ComplexFormHomographNumber = 3;

        var copy = component.Copy();

        copy.ComplexFormHomographNumber.Should().Be(3);
        copy.ComponentHomographNumber.Should().Be(2);
    }

    [Fact]
    public void Json_SkipsZeroHomographNumbers_AndRoundTripsTheRest()
    {
        var component = BankComponent();

        var json = JsonSerializer.Serialize(component);

        json.Should().NotContain(nameof(ComplexFormComponent.ComplexFormHomographNumber));
        json.Should().Contain($"\"{nameof(ComplexFormComponent.ComponentHomographNumber)}\":2");
        var roundTripped = JsonSerializer.Deserialize<ComplexFormComponent>(json)!;
        roundTripped.ComplexFormHomographNumber.Should().Be(0);
        roundTripped.ComponentHomographNumber.Should().Be(2);
    }
}
