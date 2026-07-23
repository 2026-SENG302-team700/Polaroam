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
}