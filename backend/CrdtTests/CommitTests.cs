using System.IO.Hashing;
using System.Text.Json;
using Crdt.Core;
using CrdtLib.Changes;
using CrdtSample;
using CrdtSample.Changes;
using CrdtLib.Db;
using CrdtLib.Helpers;
using Microsoft.Extensions.DependencyInjection;


namespace Tests;

public class CommitTests
{
    [Fact]
    public void CanHashWithoutParent()
    {
        var commit1 = new Commit()
        {
            ClientId = Guid.NewGuid(),
            HybridDateTime = HybridDateTime.ForTestingNow
        };
        commit1.Hash.Should().NotBeEmpty();
    }

    [Fact]
    public void SameGuidGivesSameHash()
    {
        var commit1 = new Commit()
        {
            ClientId = Guid.NewGuid(),
            HybridDateTime = HybridDateTime.ForTestingNow
        };
        var commit1Copy = new Commit(commit1.Id)
        {
            ClientId = commit1.ClientId,
            HybridDateTime = commit1.HybridDateTime
        };
        commit1.Hash.Should().Be(commit1Copy.Hash);
    }

    [Fact]
    public void SameParentGuidGivesSameHash()
    {
        var parentCommit = new Commit()
        {
            ClientId = Guid.NewGuid(),
            HybridDateTime = HybridDateTime.ForTestingNow
        };
        var commit1 = new Commit()
        {
            ClientId = Guid.NewGuid(),
            HybridDateTime = HybridDateTime.ForTestingNow
        };
        var commit1Copy = new Commit(commit1.Id)
        {
            ClientId = commit1.ClientId,
            HybridDateTime = commit1.HybridDateTime
        };
        commit1.SetParentHash(parentCommit.Hash);
        commit1Copy.SetParentHash(parentCommit.Hash);
        commit1.Hash.Should().Be(commit1Copy.Hash);
    }

    [Fact]
    public void ParentChangesHash()
    {
        var commit1 = new Commit()
        {
            ClientId = Guid.NewGuid(),
            HybridDateTime = HybridDateTime.ForTestingNow
        };
        var commit2 = new Commit()
        {
            ClientId = Guid.NewGuid(),
            HybridDateTime = HybridDateTime.ForTestingNow
        };
        var initialCommit2Hash = commit2.Hash;
        commit2.SetParentHash(commit1.Hash);
        commit2.Hash.Should().NotBe(initialCommit2Hash);
    }

    [Fact]
    public void ChangingParentChangesHash()
    {
        var commit1 = new Commit()
        {
            ClientId = Guid.NewGuid(),
            HybridDateTime = HybridDateTime.ForTestingNow
        };
        var commit2 = new Commit()
        {
            ClientId = Guid.NewGuid(),
            HybridDateTime = HybridDateTime.ForTestingNow
        };
        var commit3 = new Commit()
        {
            ClientId = Guid.NewGuid(),
            HybridDateTime = HybridDateTime.ForTestingNow
        };
        commit2.SetParentHash(commit1.Hash);
        var initialCommit2Hash = commit2.Hash;
        commit2.SetParentHash(commit3.Hash);
        commit2.Hash.Should().NotBe(initialCommit2Hash);
    }

    [Fact]
    public void CanRoundTripCommitThroughJson()
    {
        var serializerOptions = new ServiceCollection()
            .AddCrdtDataSample(":memory:")
            .BuildServiceProvider().GetRequiredService<JsonSerializerOptions>();
        var commit = new Commit
        {
            ClientId = Guid.NewGuid(),
            HybridDateTime = HybridDateTime.ForTestingNow,
            ChangeEntities =
            {
                ((IChange)new SetWordTextChange(Guid.NewGuid(), "hello")).ToChangeEntity(0)
            }
        };
        commit.SetParentHash(Convert.ToHexString(XxHash64.Hash(Guid.NewGuid().ToByteArray())));
        var json = JsonSerializer.Serialize(commit, serializerOptions);
        var commit2 = JsonSerializer.Deserialize<Commit>(json, serializerOptions);
        commit2.Should().BeEquivalentTo(commit);
    }
}
