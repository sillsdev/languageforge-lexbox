using FluentValidation.TestHelper;
using MiniLcm.Validators;

namespace MiniLcm.Tests.Validators;

public class WritingSystemValidatorTests
{
    private readonly WritingSystemValidator _validator = new();

    private static WritingSystem NewWs() => new()
    {
        Id = Guid.NewGuid(),
        WsId = "en",
        Name = "English",
        Abbreviation = "En",
        Font = "Arial",
        Type = WritingSystemType.Vernacular,
    };

    [Fact]
    public void Succeeds_WithNoCollation()
    {
        _validator.TestValidate(NewWs()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Succeeds_WithOnlyIcuCollationRules()
    {
        _validator.TestValidate(NewWs() with { IcuCollationRules = "&b < a" }).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Succeeds_WithOnlySystemCollationLocale()
    {
        _validator.TestValidate(NewWs() with { SystemCollationLocale = "de" }).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Fails_WithBothIcuCollationRulesAndSystemCollationLocale()
    {
        _validator.TestValidate(NewWs() with { IcuCollationRules = "&b < a", SystemCollationLocale = "de" })
            .ShouldHaveValidationErrorFor(ws => ws.SystemCollationLocale);
    }
}
