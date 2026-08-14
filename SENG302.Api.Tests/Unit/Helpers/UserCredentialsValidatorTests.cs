using Microsoft.AspNetCore.Identity;
using SENG302.Api.Helpers.Validators;
using SENG302.Api.Models.Entities;
using Shouldly;

namespace SENG302.Api.Tests.Unit.Helpers;

public class UserCredentialsValidatorTests : DatabaseUnitTestFixture
{

    [Theory]
    // Example from the Email Validation handout
    [InlineData("simple@example-.com")]              // hyphens can't start or end a domain name
    [InlineData(".simple.@example.com")]              // dots can't start or end the local part
    [InlineData("simple..name@example..com")]         // dots can't appear back to back in either the local part or the domain
    [InlineData("¸sı¯mρl.´e@example.com")]             // non-ASCII letters and diacritics are not supported for this course
    [InlineData("simple user@example.com")]           // spaces in local part are invalid
    [InlineData("simple@user@example.com")]           // more than one @ character is invalid
    [InlineData("simple\\ user\\@name@example.com")]  // backslashes can't be used to escape invalid characters
    [InlineData("\"simple user@name\"@example.com")]  // quoted strings are legal for the local part, but disallowed in SENG 302
    [InlineData("simple@[192.168.0.1]")]   
    
    // Extra tests from claude
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa@example.com")] // 65-character local part, over the limit
    [InlineData("simple@aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.com")]      // domain label at 64 characters, over the limit
    [InlineData("simple@example.123")]                   // top-level domain is all-numeric
    [InlineData("simple@-example.com")]                   // hyphen at the start of a domain label
    [InlineData("simple@sub.-example.com")]                // leading hyphen applies to every label, not just the first
    [InlineData("simple@my_domain.com")]                  // underscore is not a permitted domain character
    [InlineData("simple@example.com.")]                    // trailing dot creates an empty final label
    [InlineData("simple@.example.com")]                    // leading dot creates an empty first label
    [InlineData("simple,user@example.com")]                // comma is not in the permitted character set
    [InlineData("simple;user@example.com")]                // semicolon is not in the permitted character set
    [InlineData("simple(user)@example.com")]               // parentheses aren't part of the adopted local-part syntax
    [InlineData("simple@")]                                // empty domain
    [InlineData("simpleexample.com")]                      // missing @ entirely
    [InlineData("simple@@example.com")]                    // two consecutive @ characters
    [InlineData("")]                                       // empty string
    [InlineData(" simple@example.com")]                    // leading whitespace
    [InlineData("simple@example.com ")]                    // trailing whitespace
    [InlineData("simple@example.com\n")]                   // trailing newline/control character
    
    // From the wiki
    [InlineData("plainaddress")]
    [InlineData("user.example.com")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user@@example.com")]
    [InlineData("user@ex@ample.com")]
    [InlineData("user example@example.com")]
    [InlineData("user@.com")]
    [InlineData("user@com")]
    [InlineData("user@example")]
    [InlineData("user@example.")]
    [InlineData("user@-example.com")]
    [InlineData("user@example-.com")]
    [InlineData("user@example..com")]
    [InlineData("user@.example.com")]
    [InlineData("user@example.com.")] 
    [InlineData("user@exam ple.com")]
    [InlineData("user@example.123")]
    [InlineData(".user@example.com")]
    [InlineData("user.@example.com")]
    [InlineData("us..er@example.com")]
    [InlineData("user name@example.com")]
    [InlineData("user()@example.com")]
    [InlineData("user[bracket]@example.com")]
    [InlineData("\"unclosed@example.com")]
    [InlineData("\"user\"name@example.com")]
    [InlineData("user,name@example.com")]
    [InlineData("user:name@example.com")]
    [InlineData("user;name@example.com")]
    [InlineData("user@[127.0.0.1")]
    [InlineData("user@127.0.0.1]")]
    [InlineData("user@[999.999.999.999]")]
    [InlineData("user@[::1]")]
    [InlineData("user@[example.com]")]
    [InlineData("usér@example.com")]
    [InlineData("user@exаmple.com")]
    [InlineData("\"\"@example.com")]
    
    // Failed from Blackbox testing
    // [InlineData("user@gmail.c0m")] Removed because this is supposed to fail as - Digits 0 to 9, provided that top-level domain names are not all-numeric;
    public async Task ValidateEmail_MalformedFormat_ExpectErrorMessageInErrors(string email)
    {
        var errors = UserCredentialsValidator.ValidateEmail(_mockDbContextFactory.CreateDbContext(), email);

        errors.ShouldNotBeEmpty();
        errors.ShouldHaveSingleItem();
        errors.Keys.ShouldContain("email");
        errors.Values.ShouldContain("Invalid email address. Email must be in the format 'jane@doe.nz'");
    }
    
    [Theory]
    // Example from the Email Validation handout
    [InlineData("simple@example.com")]                  // standard valid email
    [InlineData("SimplE@ExampLE.COM")]                   // duplicate of the email above (case-insensitive)
    [InlineData("simple+subemail@example.com")]          // distinct from the emails above, even if many servers will send all to the same inbox
    [InlineData("elon@x.com")]                           // single-letter domain parts are accepted
    [InlineData("x@elon.com")]                           // single-letter local parts are accepted
    [InlineData("user/name@example.com")]                // slashes, among many special characters, are valid but uncommon in the local part
    [InlineData("simple@example-1.com")]                 // hyphens and digits are valid in the domain
    [InlineData("simple.user.0.1@gmail.com")]            // dots are valid (and common)
    [InlineData("simpleuser01@gmail.com")]               // distinct from the email above, even if Gmail will send both to the same inbox
    
    // Extra tests from claude
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa@example.com")] // exactly 64-character local part
    [InlineData("simple@aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.com")]      // domain label exactly at 63 characters
    [InlineData("simple@example.1a2")]                  // TLD contains a non-digit, so it isn't "all-numeric"
    [InlineData("simple@123.com")]                       // non-TLD label may be all-numeric
    [InlineData("simple@example--test.com")]             // consecutive hyphens are fine mid-label
    [InlineData("!#$%&*+-/=?^_`{|}'@example.com")]       // every permitted special character in the local part
    [InlineData("!simple@example.com")]                  // local part may start with a special character
    [InlineData("simple!@example.com")]                  // local part may end with a special character
    [InlineData("12345@example.com")]                    // purely numeric local part
    [InlineData("SIMPLE@EXAMPLE.COM")]                   // fully uppercase, normalizes to lowercase per Rule 1
    [InlineData("sImPlE@eXaMpLe.CoM")]                    // mixed case, same normalization behavior
    
    // Failed from Blackbox testing
    [InlineData("d@d.d")]
    public async Task ValidateEmail_ValidEmail_NoErrors(string email)
    {
        var errors = UserCredentialsValidator.ValidateEmail(_mockDbContextFactory.CreateDbContext(), email);

        errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("John")] // Sanity Check
    [InlineData("Mary Jane")] // Spaces
    [InlineData("Anne-Marie")] // Hyphens
    [InlineData("D'Arcy")] // Apostrophes
    [InlineData("Amy")] // 3 chars
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKL")] // 64 chars
    [InlineData("'''")] // All Apostrophes (no Unicode letters)
    public void ValidateDisplayName_ValidName_NoErrors(string name)
    {
        var errors = UserCredentialsValidator.ValidateDisplayName(name);
        errors.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("a")]
    [InlineData("bc")]
    [InlineData("this-name-is-definitely-longer-than-sixty-four-characters-for-testing-purposes")]
    public void ValidateDisplayName_InvalidLength_ExpectErrorMessageInErrors(string name)
    {
        var errors = UserCredentialsValidator.ValidateDisplayName(name);
        
        errors.ShouldNotBeEmpty();
        errors.ShouldHaveSingleItem();
        errors.Keys.ShouldContain("displayName");
        errors.Values.ShouldContain("Display name must be between 3 and 64 characters");
    }
    
    [Theory]
    [InlineData("Test_L")]
    [InlineData("Test!")]
    public void ValidateDisplayName_InvalidCharacters_ExpectErrorMessageInErrors(string name)
    {    
        var errors = UserCredentialsValidator.ValidateDisplayName(name);
        
        errors.ShouldNotBeEmpty();
        errors.ShouldHaveSingleItem();
        errors.Keys.ShouldContain("displayName");
        errors.Values.ShouldContain("Display name must only include letters, spaces, hyphens or apostrophes");
    }

    [Fact]
    public void ValidateDisplayName_OnlyNumbers_ExpectErrorMessageInErrors()
    { 
        var errors = UserCredentialsValidator.ValidateDisplayName("12345");
        
        errors.ShouldNotBeEmpty();
        errors.ShouldHaveSingleItem();
        errors.Keys.ShouldContain("displayName");
        errors.Values.ShouldContain("Display name must only include letters, spaces, hyphens or apostrophes");
    }
    
    [Fact]
    public void ValidatePassword_MismatchedPasswords_ExpectErrorMessageInErrors()
    {
        var errors = UserCredentialsValidator.ValidatePassword("Password123!", "Password321!");
        
        errors.ShouldNotBeEmpty();
        errors.ShouldHaveSingleItem();
        errors.Keys.ShouldContain("passwordConfirm");
        errors.Values.ShouldContain("Passwords do not match");
    }
    
    [Theory]
    [InlineData("weak")]
    [InlineData("NoSpecialChars123")]
    [InlineData("nosymbolsorupper123")]
    public void ValidatePassword_WeakPassword_ExpectErrorMessageInErrors(string password)
    {
        var errors = UserCredentialsValidator.ValidatePassword(password, password);
        
        errors.ShouldNotBeEmpty();
        errors.ShouldHaveSingleItem();
        errors.Keys.ShouldContain("password");
        errors.Values.ShouldContain("Password must be at least 8 characters long including at least one of each uppercase, lowercase, numbers and special characters"); 
    }

    [Fact]
    public void ValidateUpdatePasswordRequest_ValidData_ReturnTrue()
    {
        var user = new User
        {
            Email = "test@example.com",
            DisplayName = "Test",
            Country = "NZ",
        };
        PasswordHasher<User> passwordHasher = new();
        var passwordKey = passwordHasher.HashPassword(user, "Team700!");
        user.PasswordKey = passwordKey;

        var valid = UserCredentialsValidator.ValidateUpdatePasswordRequest(user, "Team700!", "Team701!", "Team701!");
        valid.ShouldBeTrue();
    }
    
    [Fact]
    public void ValidateUpdatePasswordRequest_CurrentPasswordNotCorrect_ThrowMismatchedPasswordException()
    {
        var user = new User
        {
            Email = "test@example.com",
            DisplayName = "Test",
            Country = "NZ",
        };
        PasswordHasher<User> passwordHasher = new();
        var passwordKey = passwordHasher.HashPassword(user, "Team700!");
        user.PasswordKey = passwordKey;

        Should.Throw<MismatchedPasswordException>(() =>
            UserCredentialsValidator.ValidateUpdatePasswordRequest(user, "no match", "Team701!", "Team701!"));
    }
    
    [Fact]
    public void ValidateUpdatePasswordRequest_NewPasswordsDontMatch_ThrowMismatchedPasswordException()
    {
        var user = new User
        {
            Email = "test@example.com",
            DisplayName = "Test",
            Country = "NZ",
        };
        PasswordHasher<User> passwordHasher = new();
        var passwordKey = passwordHasher.HashPassword(user, "Team700!");
        user.PasswordKey = passwordKey;

        Should.Throw<MismatchedPasswordException>(() =>
            UserCredentialsValidator.ValidateUpdatePasswordRequest(user, "Team700!", "Team701!", "no match"));
    }
    
    [Fact]
    public void ValidateUpdatePasswordRequest_WeakPassword_ThrowInvalidPassword()
    {
        var user = new User
        {
            Email = "test@example.com",
            DisplayName = "Test",
            Country = "NZ",
        };
        PasswordHasher<User> passwordHasher = new();
        var passwordKey = passwordHasher.HashPassword(user, "Team700!");
        user.PasswordKey = passwordKey;

        Should.Throw<InvalidPasswordException>(() =>
            UserCredentialsValidator.ValidateUpdatePasswordRequest(user, "Team700!", "weakPassword", "weakPassword"));
    }
    
    [Fact]
    public void ValidateUpdatePasswordRequest_NewPasswordSameAsOld_ThrowArgumentException()
    {
        var user = new User
        {
            Email = "test@example.com",
            DisplayName = "Test",
            Country = "NZ",
        };
        PasswordHasher<User> passwordHasher = new();
        var passwordKey = passwordHasher.HashPassword(user, "Team700!");
        user.PasswordKey = passwordKey;

        Should.Throw<ArgumentException>(() =>
            UserCredentialsValidator.ValidateUpdatePasswordRequest(user, "Team700!", "Team700!", "Team700!"));
    }
}