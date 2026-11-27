using System.Data;
using LcmCrdt.Utils;
using Microsoft.EntityFrameworkCore;

namespace LcmCrdt.Tests.Utils;

public class InReadTransactionTests : IAsyncLifetime
{
    private readonly MiniLcmApiFixture _fixture = MiniLcmApiFixture.Create();

    public Task InitializeAsync() => _fixture.InitializeAsync();
    public Task DisposeAsync() => _fixture.DisposeAsync();

    private async Task<LcmCrdtDbContext> NewDbContext() =>
        await _fixture.GetService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();

    [Fact]
    public async Task OnAClosedConnection_ReadsAndClosesItAgain()
    {
        await using var dbContext = await NewDbContext();
        var connection = dbContext.Database.GetDbConnection();
        connection.State.Should().Be(ConnectionState.Closed);

        var stateDuringRead = await dbContext.Database.InReadTransaction(() => Task.FromResult(connection.State));

        stateDuringRead.Should().Be(ConnectionState.Open);
        connection.State.Should().Be(ConnectionState.Closed);
    }

    [Fact]
    public async Task OnAnOpenConnection_LeavesItOpen()
    {
        await using var dbContext = await NewDbContext();
        await dbContext.Database.OpenConnectionAsync();

        await dbContext.Database.InReadTransaction(() => Task.FromResult(0));

        dbContext.Database.GetDbConnection().State.Should().Be(ConnectionState.Open);
        await dbContext.Database.CloseConnectionAsync();
        dbContext.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed, "the read left no extra open behind");
    }

    [Fact]
    public async Task WhenTheReadThrows_ClosesTheConnection()
    {
        await using var dbContext = await NewDbContext();

        var read = () => dbContext.Database.InReadTransaction<int>(() => throw new InvalidOperationException("read failed"));

        await read.Should().ThrowAsync<InvalidOperationException>();
        dbContext.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed);
    }
}
