using MiniLcm.SyncHelpers;
using Moq;

namespace MiniLcm.Tests;

public class DiffCollectionTests
{
    [Fact]
    public async void FirstTest()
    {
        var api = new Mock<IMiniLcmApi>();
        var changeCount = await DiffCollection.DiffOrderable(api.Object, new List<IOrderable>(), new List<IOrderable>(),
            (value) => value.Id,
            (_api, value, newI, stable) => Task.FromResult(1),
            (_api, value) => Task.FromResult(1),
            (_api, value, newI, stable) => Task.FromResult(1),
            (_api, oldValue, newValue) => Task.FromResult(1));

        changeCount.Should().Be(0);
        api.VerifyNoOtherCalls();
    }

    // [Theory]
    // [InlineData("gx")]
    // [InlineData("oo")]
    // [InlineData("eng")] // Three-letter codes not allowed when there's a valid two-letter code
    // [InlineData("eng-Zxxx-x-audio")]
    // [InlineData("nonsense")]
    // public void InvalidWritingSystemId_ShouldThrow(string code)
    // {
    //     Assert.Throws<ArgumentException>(() => new WritingSystemId(code));
    // }
}
