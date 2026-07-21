using Microsoft.AspNetCore.Identity;
using SENG302.Api.Models.Entities;
using Shouldly;

namespace SENG302.Api.Tests.Unit.Helpers;

public class TripValidatorTests
{
    [Theory]
    [InlineData("ab")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task ValidTitleLength_InvalidLength_ReturnsFalse(string title)
    {
        TripValidator.ValidTitleLength(title).ShouldBeFalse();
    }
    
    [Theory]
    [InlineData("abc")] // Letters
    [InlineData("a b")] // Spaces
    [InlineData("a-b")] // Hyphens
    [InlineData("a'b")] // Apostrophes
    [InlineData("a2b")] // Numbers
    public async Task ValidTitleChars_ValidChars_ReturnsTrue(string title)
    {
        TripValidator.ValidTitleChars(title).ShouldBeTrue();
    }
    
    [Theory]
    [InlineData("ab!")]
    [InlineData("ab@")]
    [InlineData("a_b")]
    public async Task ValidTitleChars_InvalidChars_ReturnsFalse(string title)
    {
        TripValidator.ValidTitleChars(title).ShouldBeFalse();
    }
    
    [Fact]
    public async Task ValidateTile_ValidTitle_ReturnsNothing()
    {
        Should.NotThrow(() => TripValidator.ValidateTitle("Test"));
    }

    [Fact]
    public async Task ValidateTile_ShortAndInvalidChars_MultipleErrors()
    {
        var errors = TripValidator.ValidateTitle("a!");
        errors["title"].Count.ShouldBe(2);
        errors["title"].ShouldContain("Trip title must be between 3 and 128 characters long");
        errors["title"].ShouldContain("Trip title cannot contain characters other than letters, spaces, hyphens, apostrophes, or numbers");
    }

    [Fact]
    public async Task ValidateTile_Empty_ReturnsFalse()
    {
        var errors = TripValidator.ValidateTitle("");
        errors["title"].Count.ShouldBe(1);
        errors["title"].ShouldContain("Trip title is required");
    }
}