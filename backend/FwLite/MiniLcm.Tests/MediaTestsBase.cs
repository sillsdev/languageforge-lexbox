using System.Net.Mime;
using System.Text;
using MiniLcm.Media;

namespace MiniLcm.Tests;

public abstract class MediaTestsBase : MiniLcmTestBase
{
    [Fact]
    public async Task FileOperations_TextFile_RoundTripSuccess()
    {
        // Arrange
        const string testContent = "This is a test file content with special characters: áéíóú, 中文, 🚀";
        const string fileName = "test-file.txt";
        const string mimeType = "text/plain";
        const string author = "Test Author";
        var uploadDate = DateTimeOffset.UtcNow;

        var originalBytes = Encoding.UTF8.GetBytes(testContent);
        var metadata = new LcmFileMetadata(fileName, mimeType, author, uploadDate);

        // Act - Save the file
        UploadFileResponse saveResponse;
        await using (var saveStream = new MemoryStream(originalBytes))
        {
            saveResponse = await Api.SaveFile(saveStream, metadata);
        }

        saveResponse.ErrorMessage.Should().BeNullOrEmpty();
        saveResponse.Result.Should().BeOneOf(UploadFileResult.SavedLocally, UploadFileResult.SavedToLexbox);
        saveResponse.MediaUri.Should().NotBeNull().And.NotBe(MediaUri.NotFound);

        // Act - Retrieve the file
        var retrieveResponse = await Api.GetFileStream(saveResponse.MediaUri.Value);

        // Assert - Verify retrieval was successful
        retrieveResponse.Result.Should().Be(ReadFileResult.Success);
        retrieveResponse.ErrorMessage.Should().BeNullOrEmpty();
        retrieveResponse.Stream.Should().NotBeNull();
        retrieveResponse.FileName.Should().Be(fileName);

        // Assert - Verify content integrity
        byte[] retrievedBytes;
        await using (retrieveResponse.Stream)
        {
            using var memoryStream = new MemoryStream();
            await retrieveResponse.Stream.CopyToAsync(memoryStream);
            retrievedBytes = memoryStream.ToArray();
        }

        retrievedBytes.Length.Should().Be(originalBytes.Length,
            "Retrieved binary content should have the same length as original");
        retrievedBytes.Should()
            .BeEquivalentTo(originalBytes, "Retrieved content should match original content exactly");

        var retrievedContent = Encoding.UTF8.GetString(retrievedBytes);
        retrievedContent.Should().Be(testContent, "Retrieved text content should match original text content");
    }

    [Fact]
    public async Task FileOperations_BinaryFile_RoundTripSuccess()
    {
        // Arrange - Create test binary data (simulating a small image or audio file)
        var originalBytes = new byte[1024];
        var random = new Random(42); // Use fixed seed for reproducible tests
        random.NextBytes(originalBytes);

        const string fileName = "test-binary-file.dat";
        const string author = "Binary Test Author";
        var uploadDate = DateTimeOffset.UtcNow;

        var metadata = new LcmFileMetadata(fileName, MediaTypeNames.Application.Octet, author, uploadDate);

        // Act - Save the binary file
        UploadFileResponse saveResponse;
        await using (var saveStream = new MemoryStream(originalBytes))
        {
            saveResponse = await Api.SaveFile(saveStream, metadata);
        }

        saveResponse.ErrorMessage.Should().BeNullOrEmpty();
        saveResponse.Result.Should().BeOneOf(UploadFileResult.SavedLocally, UploadFileResult.SavedToLexbox);
        saveResponse.MediaUri.Should().NotBeNull().And.NotBe(MediaUri.NotFound);

        // Act - Retrieve the binary file
        var retrieveResponse = await Api.GetFileStream(saveResponse.MediaUri.Value);

        // Assert - Verify retrieval was successful
        retrieveResponse.Result.Should().Be(ReadFileResult.Success);
        retrieveResponse.ErrorMessage.Should().BeNullOrEmpty();
        retrieveResponse.Stream.Should().NotBeNull();
        retrieveResponse.FileName.Should().Be(fileName);

        // Assert - Verify binary content integrity
        byte[] retrievedBytes;
        await using (var retrievedStream = retrieveResponse.Stream)
        {
            using var memoryStream = new MemoryStream();
            await retrievedStream.CopyToAsync(memoryStream);
            retrievedBytes = memoryStream.ToArray();
        }

        retrievedBytes.Length.Should().Be(originalBytes.Length,
            "Retrieved binary content should have the same length as original");
        retrievedBytes.Should().BeEquivalentTo(originalBytes,
            "Retrieved binary content should match original binary content exactly");
    }

    [Fact]
    public async Task FileOperations_FileTooLarge_FailsGracefully()
    {
        // Arrange - Create test binary data (simulating a small image or audio file)
        var originalBytes = new byte[1024 * 1024 * 25]; // 25 MB
        var random = new Random(42); // Use fixed seed for reproducible tests
        random.NextBytes(originalBytes);

        var saveResponse = await SaveTestFile("test-binary-file.dat", originalBytes);

        saveResponse.Result.Should().Be(UploadFileResult.TooBig);
        saveResponse.MediaUri.Should().BeNull();
    }

    [Fact]
    public async Task FileOperations_SameNameDifferentContent_SavesASeparateFile()
    {
        const string fileName = "IMG_0001.jpg";
        var firstBytes = new byte[1024];
        var secondBytes = new byte[1024];
        Random.Shared.NextBytes(firstBytes);
        Random.Shared.NextBytes(secondBytes);

        var first = await SaveTestFile(fileName, firstBytes, MediaTypeNames.Image.Jpeg);
        first.Result.Should().Be(UploadFileResult.SavedLocally);

        var second = await SaveTestFile(fileName, secondBytes, MediaTypeNames.Image.Jpeg);
        second.Result.Should().Be(UploadFileResult.SavedLocally);
        second.MediaUri.Should().NotBeNull().And.NotBe(MediaUri.NotFound).And.NotBe(first.MediaUri);

        (await ReadAllBytes(first.MediaUri!.Value)).Should().Equal(firstBytes);
        (await ReadAllBytes(second.MediaUri!.Value)).Should().Equal(secondBytes);
    }

    [Fact]
    public async Task FileOperations_SameNameSameContent_ReturnsTheExistingFile()
    {
        const string fileName = "test-binary-file.dat";
        var bytes = new byte[1024];
        Random.Shared.NextBytes(bytes);

        var first = await SaveTestFile(fileName, bytes);
        first.Result.Should().Be(UploadFileResult.SavedLocally);

        var second = await SaveTestFile(fileName, bytes);
        second.Result.Should().Be(UploadFileResult.AlreadyExists);
        second.MediaUri.Should().Be(first.MediaUri);
    }

    protected async Task<byte[]> ReadAllBytes(MediaUri mediaUri)
    {
        var response = await Api.GetFileStream(mediaUri);
        response.Result.Should().Be(ReadFileResult.Success);
        await using var stream = response.Stream!;
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    protected async Task<UploadFileResponse> SaveTestFile(string fileName, byte[]? originalBytes = null, string mimeType = MediaTypeNames.Application.Octet)
    {
        const string author = "Test Author";
        var uploadDate = DateTimeOffset.UtcNow;
        if (originalBytes is null) Random.Shared.NextBytes(originalBytes = new byte[1024]);

        var metadata = new LcmFileMetadata(fileName, mimeType, author, uploadDate);

        // Act - Save the binary file
        UploadFileResponse saveResponse;
        await using (var saveStream = new MemoryStream(originalBytes))
        {
            saveResponse = await Api.SaveFile(saveStream, metadata);
        }

        return saveResponse;
    }


    [Fact]
    public async Task GetFileStream_NonExistentFile_HandlesGracefully()
    {
        // Test retrieving a non-existent file
        var nonExistentMediaUri = new MediaUri(Guid.NewGuid(), "localhost");//fw only supports localhost
        var response = await Api.GetFileStream(nonExistentMediaUri);

        // Should handle gracefully without throwing
        response.Should().NotBeNull();
        response.Result.Should().BeOneOf(ReadFileResult.NotFound, ReadFileResult.Offline);
        response.Stream.Should().BeNull();
    }
}
