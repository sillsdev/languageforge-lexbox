using System.Text;
using MiniLcm.Media;
using Xunit.Abstractions;

namespace MiniLcm.Tests;

public abstract class MediaTestsBase : MiniLcmTestBase
{
    protected readonly ITestOutputHelper Output;

    protected MediaTestsBase(ITestOutputHelper output)
    {
        Output = output;
    }

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

        saveResponse.Result.Should().BeOneOf(UploadFileResult.SavedLocally, UploadFileResult.SavedToLexbox);
        saveResponse.ErrorMessage.Should().BeNullOrEmpty();
        saveResponse.MediaUri.Should().NotBeNull();

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
        const string mimeType = "application/octet-stream";
        const string author = "Binary Test Author";
        var uploadDate = DateTimeOffset.UtcNow;

        var metadata = new LcmFileMetadata(fileName, mimeType, author, uploadDate);

        // Act - Save the binary file
        UploadFileResponse saveResponse;
        await using (var saveStream = new MemoryStream(originalBytes))
        {
            saveResponse = await Api.SaveFile(saveStream, metadata);
        }

        saveResponse.Result.Should().BeOneOf(UploadFileResult.SavedLocally, UploadFileResult.SavedToLexbox);
        saveResponse.ErrorMessage.Should().BeNullOrEmpty();
        saveResponse.MediaUri.Should().NotBeNull();

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
        saveResponse.ErrorMessage.Should().NotBeNullOrEmpty();
        saveResponse.MediaUri.Should().BeNull();
    }

    [Fact]
    public async Task FileOperations_FileNameConflict_HandlesGracefully()
    {

        const string fileName = "test-binary-file.dat";

        var saveResponse = await SaveTestFile(fileName);
        var firstMediaUri = saveResponse.MediaUri;
        saveResponse.Result.Should().Be(UploadFileResult.SavedLocally);

        // Act - Save a different file with the same name
        saveResponse = await SaveTestFile(fileName);
        saveResponse.Result.Should().Be(UploadFileResult.SavedLocally);
        saveResponse.MediaUri.Should().NotBe(firstMediaUri);
        var retrieveResponse = await Api.GetFileStream(saveResponse.MediaUri.Value);
        retrieveResponse.FileName.Should().Be("test-binary-file-1.dat");
        retrieveResponse.Stream?.Dispose();
    }

    private async Task<UploadFileResponse> SaveTestFile(string fileName, byte[]? originalBytes = null, string mimeType = "application/octet-stream")
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
