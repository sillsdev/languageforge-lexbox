using FluentValidation;
using MiniLcm.Validators;

namespace MiniLcm.Tests.Validators;

public class PublicationUpdateValidatorTests
{
    private readonly PublicationUpdateValidator _validator = new();

    [Fact]
    public async Task RejectsTurningOffIsMain()
    {
        var act = () => _validator.ValidateAndThrowAsync(new UpdateObjectInput<Publication>().Set(p => p.IsMain, false));
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task RejectsChangingId()
    {
        var act = () => _validator.ValidateAndThrowAsync(new UpdateObjectInput<Publication>().Set(p => p.Id, Guid.NewGuid()));
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task RejectsChangingDeletedAt()
    {
        var act = () => _validator.ValidateAndThrowAsync(new UpdateObjectInput<Publication>().Set(p => p.DeletedAt, DateTimeOffset.UtcNow));
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task RejectsEmptyPatch()
    {
        var act = () => _validator.ValidateAndThrowAsync(new UpdateObjectInput<Publication>());
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AllowsPromotingToMainAndNameUpdates()
    {
        await _validator.ValidateAndThrowAsync(new UpdateObjectInput<Publication>().Set(p => p.IsMain, true));
        await _validator.ValidateAndThrowAsync(new UpdateObjectInput<Publication>().Set(p => p.Name["en"], "Updated"));
    }
}
