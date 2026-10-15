using System.Text;
using FwLiteShared.LocalSync;

namespace LcmCrdt.Tests.LocalSync;

public class FolderMediaStoreTests
{
    private static readonly Guid ResourceId = Guid.Parse("1a3c5e7b-9d0f-4b2d-8f4a-6c8e0b2d4f91");

    private static async Task<string> PutAndGetName(FolderMediaStore store, string fileName)
    {
        await store.Put(ResourceId, new MemoryStream(Encoding.UTF8.GetBytes("bytes")), fileName);
        await using var stored = await store.Get(ResourceId);
        return stored!.FileName;
    }

    [Fact]
    public async Task AFileNameIllegalOnSomePlatforms_IsMadeSafe()
    {
        // legal on Linux, but not on Windows or on exFAT/FAT32 USB drives
        using var folder = new TempFolder();

        var name = await PutAndGetName(new FolderMediaStore(folder.Path), "interview 3:2?.wav");

        name.Should().NotContainAny(":", "?").And.EndWith(".wav");
    }

    [Fact]
    public async Task AFileWhoseRealNameEndsInTmp_IsStored()
    {
        using var folder = new TempFolder();
        var store = new FolderMediaStore(folder.Path);

        var name = await PutAndGetName(store, "session.tmp");

        name.Should().Be("session.tmp");
        (await store.ResourceIds()).Should().Equal(ResourceId);
    }
}
