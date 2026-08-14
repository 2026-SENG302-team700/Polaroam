using Microsoft.AspNetCore.Http;
using SENG302.Api.Helpers;
using SENG302.Api.Helpers.Validators;
using Shouldly;

namespace SENG302.Api.Tests.Unit.Services;

public class ImageTypeValidationTests
{

    private IFormFile GetMockFile(string filename, Byte[] content, string? mimeType = null)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, stream.Length, "file", filename)
        {
            Headers = new HeaderDictionary(),
            ContentType = mimeType!
        };
    }

    [Theory]
    [InlineData(new byte[] { 0x89, 0x50, 0x4e, 0x47 }, "image/png")]
    [InlineData(new byte[] { 0x47, 0x49, 0x46, 0x38 }, "image/gif")]
    [InlineData(new byte[] { 0xff, 0xd8, 0xff, 0xe0 }, "image/jpeg")]
    [InlineData(new byte[] { 0xff, 0xd8, 0xff, 0xe1 }, "image/jpeg")]
    [InlineData(new byte[] { 0xff, 0xd8, 0xff, 0xe2 }, "image/jpeg")]
    [InlineData(new byte[] { 0xff, 0xd8, 0xff, 0xe3 }, "image/jpeg")]
    [InlineData(new byte[] { 0xff, 0xd8, 0xff, 0xe8 }, "image/jpeg")]
    [InlineData(new byte[] { 0x3c, 0x73, 0x76, 0x67 }, "image/svg+xml")]
    [InlineData(new byte[] { 0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50 }, "image/webp")]
    public async Task GetMimeOfFile_ValidImageButJustMime_Success(byte[] bytes, string expectedMime)
    {
        var file = GetMockFile("test", bytes);
        ImageValidator.GetRealImageMime(file).ShouldBe(expectedMime);
    }

    [Theory]
    [InlineData(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x56, 0x34 }, "image/png")]
    [InlineData(new byte[] { 0x47, 0x49, 0x46, 0x38, 0xff, 0xf4, 0x00 }, "image/gif")]
    [InlineData(new byte[] { 0xff, 0xd8, 0xff, 0xe0, 0xf3 }, "image/jpeg")]
    [InlineData(new byte[] { 0x52, 0x49, 0x46, 0x46, 0xbb, 0xff, 0x45, 0x3a, 0x57, 0x45, 0x42, 0x50 }, "image/webp")]
    [InlineData(new byte[] { 0x52, 0x49, 0x46, 0x46, 0xfa, 0xaf, 0x45, 0x1f, 0x57, 0x45, 0x42, 0x50, 0x56, 0xff }, "image/webp")]
    [InlineData(new byte[] { 0x52, 0x49, 0x46, 0x46, 0xfa, 0xaf, 0x45, 0x1f, 0x57, 0x45, 0x42, 0x50, 0x56 }, "image/webp")]
    public async Task GetMimeOfFile_ValidImage_Success(byte[] bytes, string expectedMime)
    {
        var file = GetMockFile("test", bytes);
        ImageValidator.GetRealImageMime(file).ShouldBe(expectedMime);
    }
    
    [Theory]
    [InlineData(new byte[] { 0x89, 0x50, 0x4e })]
    [InlineData(new byte[] { 0x89, 0x50, 0x4e, 0x00 })]
    [InlineData(new byte[] { 0xff, 0xd8, 0xff })]
    [InlineData(new byte[] { 0x52, 0x49, 0x46, 0x46 })]
    [InlineData(new byte[] { 0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42 })]
    public async Task GetMimeOfFile_InvalidImage_Fail(byte[] bytes)
    {
        var file = GetMockFile("test", bytes);
        ImageValidator.GetRealImageMime(file).ShouldBe(ImageValidator.InvalidMimeString);
    }

    [Theory] 
    [InlineData(10000000)] // at 10MB
    [InlineData(5000000)] // below 10MB
    public async Task IsValidFilesize_AtOrUnderLimit_Success(long fileSize)
    {
        var file = GetMockFile("test", new byte[fileSize]);
        ImageValidator.IsValidFilesize(file).ShouldBeTrue();
    }

    [Fact]
    public async Task IsValidFilesize_ExceedsLimit_Fail()
    {
        var file = GetMockFile("test", new byte[10000001]);
        ImageValidator.IsValidFilesize(file).ShouldBeFalse();
    }
    
    [Fact] 
    public async Task IsValidMimeType_ValidMimeNoSpoof_Success()
    {
        var file = GetMockFile("test", new byte[] { 0x89, 0x50, 0x4e, 0x47 }, "image/png"); 
        ImageValidator.IsValidMimeType(file).ShouldBeTrue();
    }

    [Fact]
    public async Task IsValidMimeType_ValidMimeInvalidSpoof_Fail()
    {
        var file = GetMockFile("test", new byte[] { 0x00, 0x00, 0x00, 0x00 }, "image/png");
        ImageValidator.IsValidMimeType(file).ShouldBeFalse();
    }

    [Fact]
    public async Task IsValidMimeType_InvalidMimeValidSpoof_Fail()
    {
        var file = GetMockFile("test", new byte[] { 0x89, 0x50, 0x4e, 0x47 }, "application/octet-stream");
        ImageValidator.IsValidMimeType(file).ShouldBeFalse();
    }
}