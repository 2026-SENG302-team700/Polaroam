using Shouldly;

namespace SENG302.Api.Tests.Unit.Helpers;

public class PostValidatorTests
{
    [Fact]
    public async Task ValidatePostDate_PastDate_ReturnNoErrors()
    {
        var errors = PostValidator.ValidatePostDate(DateTime.Now.AddDays(-1));
        errors.Count.ShouldBe(0);
    }
    
    [Fact]
    public async Task ValidatePostDate_FutureDate_ReturnCorrectError()
    {
        var errors = PostValidator.ValidatePostDate(DateTime.Now.AddDays(1));
        errors.Count.ShouldBe(1);
        errors.ShouldContain("Invalid date, date cannot be in the future");
    }
    
    [Fact]
    public async Task ValidatePostDate_NullDate_ReturnCorrectError()
    {
        var errors = PostValidator.ValidatePostDate(null);
        errors.Count.ShouldBe(1);
        errors[0].ShouldContain("Invalid date, date must be formatted ");   
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
}