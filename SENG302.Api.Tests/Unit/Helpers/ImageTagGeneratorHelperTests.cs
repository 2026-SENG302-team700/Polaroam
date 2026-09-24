using Microsoft.AspNetCore.Http;
using SENG302.Api.Helpers;
using Shouldly;

namespace SENG302.Api.Tests.Unit.Helpers;

public class ImageTagGeneratorHelperTests
{
    private IFormFile GetMockFile(string filename, byte[] content, string? mimeType = null)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, stream.Length, "file", filename)
        {
            Headers = new HeaderDictionary(),
            ContentType = mimeType!
        };
    }


    [Fact]
    public async Task IsValidFile_ValidFile_ContentPreservedSuccess()
    {
        byte[] bytes = new byte[] { 0x89, 0x50, 0x4e, 0x47 };
        var file = GetMockFile("test", bytes);
        FormFile newFile = await ImageTagGeneratorHelper.createFormFileForTagging(file);
        // using var oldStream = file.OpenReadStream();
        byte[] newFileBytes;
        using var newStream = newFile.OpenReadStream();
        using (var memoryStream = new MemoryStream())
        {
            // CopyToAsync chunks the data efficiently into memory without blocking
            await newStream.CopyToAsync(memoryStream);
            newFileBytes = memoryStream.ToArray();
        }
        newFileBytes.ShouldBeEquivalentTo(bytes);
        newFileBytes.Length.ShouldBe(bytes.Length);
    }

    [Fact]
    public async Task IsValidFile_PositionAt0_Success()
    {
        byte[] bytes = new byte[] { 0x89, 0x50, 0x4e, 0x47 };

        var file = GetMockFile("test", bytes);
        FormFile newFile = await ImageTagGeneratorHelper.createFormFileForTagging(file);

        using var newStream = newFile.OpenReadStream();

        newStream.Position.ShouldBe(0);
    }

    [Fact]
    public async Task IsValidData_EmptyFileZeroBytes_Success()
    {
        byte[] bytes = new byte[] { };
        var file = GetMockFile("test", bytes);

        await Should.NotThrowAsync(async () =>
        {
            await ImageTagGeneratorHelper.createFormFileForTagging(file);
        });
    }

    [Fact]
    public async Task IsValidData_NullFilenameNullContentType_Success()
    {
        byte[] bytes = new byte[] { };
        var stream = new MemoryStream(bytes);

        var file = new FormFile(stream, 0, stream.Length, "", "")
        {
            Headers = new HeaderDictionary(),
            ContentType = ""
        };

        await Should.NotThrowAsync(async () =>
        {
            await ImageTagGeneratorHelper.createFormFileForTagging(file);
        });
    }
}