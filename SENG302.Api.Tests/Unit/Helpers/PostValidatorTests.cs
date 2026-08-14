using SENG302.Api.Helpers.Validators;
using Shouldly;

namespace SENG302.Api.Tests.Unit.Helpers;

public class PostValidatorTests
{
    [Fact]
    public async Task ValidatePostDate_PastDate_ReturnNoErrors()
    {
        var errors = PostValidator.ValidatePostDate(DateTimeOffset.UtcNow.AddDays(-1));
        errors.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ValidatePostDate_FutureDate_ReturnCorrectError()
    {
        var errors = PostValidator.ValidatePostDate(DateTimeOffset.UtcNow.AddDays(1));
        errors.Count.ShouldBe(1);
        errors.ShouldContain("Invalid date, date cannot be in the future");
    }

    [Fact]
    public async Task ValidatePostDate_NullDate_ReturnCorrectError()
    {
        var errors = PostValidator.ValidatePostDate(null);
        errors.Count.ShouldBe(1);
        errors[0].ShouldContain("Date is invalid");
    }

    [Fact]
    public async Task ValidatePostContent_NoContentDisallowed_ReturnCorrectError()
    {
        var errrors = PostValidator.ValidatePostContent("", false);
        errrors.Count.ShouldBe(1);
        errrors[0].ShouldContain("Post contains no content, are you sure you want to create post?");
    }

    [Fact]
    public async Task ValidatePostContent_NoContentAllowed_ReturnCorrectError()
    {
        var errrors = PostValidator.ValidatePostContent("", true);
        errrors.Count.ShouldBe(0);
    }

    [Fact]
    public void ValidatePostContent_ContentAtMaxLength_ReturnsNoErrors()
    {
        var errors = PostValidator.ValidatePostContent(new string('a', 8196), false);
        errors.Count.ShouldBe(0);
    }

    [Fact]
    public void ValidatePostContent_ContentOverMaxLength_ReturnsError()
    {
        var errors = PostValidator.ValidatePostContent(new string('a', 8197), false);
        errors.Count.ShouldBe(1);
        errors.ShouldContain("Content needs to be less than 8196 characters long");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValidatePostLocation_InvalidOsmId_ReturnsError(long osmId)
    {
        var errors = PostValidator.ValidatePostLocation(osmId);

        errors.ShouldContain("This isn't a valid location");
    }

    [Fact]
    public void ValidatePostLocation_ValidOsmId_ReturnsNoErrors()
    {
        var errors = PostValidator.ValidatePostLocation(123);

        errors.ShouldBeEmpty();
    }
}
