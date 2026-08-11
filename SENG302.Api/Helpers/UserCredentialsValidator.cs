using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using SENG302.Api.Constants;
using SENG302.Api.DataAccess;
using SENG302.Api.Models.Entities;
using SENG302.Api.Services;

namespace SENG302.Api.Helpers;

public class UserCredentialsValidator
{
    /// <summary>
    /// Checks to see if display name length is between 3-64 characters
    /// </summary>
    /// <param name="displayName">String representing display name of the user</param>
    /// <returns>
    /// True: If display name is between 3-64 characters
    /// False: If display name is less than 3 characters or greater than 64 characters
    /// </returns>
    public static bool DisplayNameLength(string displayName)
    {
        return (displayName.Length < 3) || (displayName.Length > 64);
    }

    /// <summary>
    /// Checks to see if the display name characters are valid
    /// </summary>
    /// <param name="displayName">String representing display name of the user</param>
    /// <returns>
    /// True: If display name only contains allowed characters
    /// False: If display name contains any disallowed characters
    /// </returns>
    public static bool DisplayNameChars(string displayName)
    {
        return ValidationPatterns.UserDisplayName.IsMatch(displayName);
    }

    /// <summary>
    /// Checks to see if the e-mail string is in a valid format
    /// </summary>
    /// <param name="email"> String representing e-mail of the user</param>
    /// <returns>
    /// True: E-mail is a valid format
    /// False: E-mail is an invalid format
    /// </returns>
    public static bool CheckEmailFormat(string email)
    {
        if (string.IsNullOrEmpty(email)) return false;

        int atIndex = email.IndexOf('@'); // returns -1 if not found
        if (atIndex <= 0 || atIndex != email.LastIndexOf('@')) return false;

        string local = email.Substring(0, atIndex);
        string domain = email.Substring(atIndex + 1);

        if (local.Length == 0 || local.Length > 64) return false;
        if (domain.Length == 0 || domain.Length > 255) return false;

        if (!email.All(c => c < 128)) return false; // ASCII only

        const string localAtom = @"[A-Za-z0-9!#$%&*+\-/=?^_`'{|}]+";
        if (!Regex.IsMatch(local, $@"\A{localAtom}(\.{localAtom})*\z")) return false;

        string[] labels = domain.Split('.');
        if (labels.Length < 2) return false; // require domain + top level domain

        foreach (var label in labels)
        {
            if (label.Length == 0 || label.Length > 63) return false;
            if (!Regex.IsMatch(label, @"\A[A-Za-z0-9]([A-Za-z0-9-]{0,61}[A-Za-z0-9])?\z")) return false;
        }

        if (Regex.IsMatch(labels[^1], @"\A\d+\z")) return false; // TLD can't be all-numeric

        return true;
    }

    /// <summary>
    /// Checks to see if password meets strength requirements
    /// </summary>
    /// <param name="password">Plaintext string representation of the users password</param>
    /// <returns>
    /// True: Password meets password strength requirements
    /// False: Password does not meet password strength requirements
    /// </returns>
    public static bool CheckPassword(string password)
    {
        return ValidationPatterns.UserPassword.IsMatch(password);
    }

    /// <summary>
    /// Checks to see if the two provided passwords are matching
    /// </summary>
    /// <param name="passwordString">Plaintext string representation of the users password</param>
    /// <param name="passwordConfirmation">Plaintext string representation of the password confirmation</param>
    /// <returns>
    /// True: passwords match
    /// False: passwords differ
    /// </returns>
    public static bool PasswordMatching(string passwordString, string passwordConfirmation)
    {
        return passwordString == passwordConfirmation;
    }
    /// <summary>
    /// Runs all email validation checks and throws relavent exceptions
    /// </summary>
    /// <param name="context">The database - checks to see if email in db already.</param>
    /// <param name="email">E-mail to be validated</param>
    public static Dictionary<string, string> ValidateEmail(DatabaseContext context, string email)
    {
        var errors = new Dictionary<string, string>();

        if (context.Users.Where((t) => t.Email.ToLower().Equals(email.ToLower())).Any())
        {
            errors["email"] = "This email address is already in use by another account";
        }

        if (!CheckEmailFormat(email))
        {
            errors["email"] = "Invalid email address. Email must be in the format 'jane@doe.nz'";
        }
        return errors;
    }

    /// <summary>
    /// Runs all display name validations and throws relavent exceptions
    /// </summary>
    /// <param name="displayName">Display Name to be validated</param>
    public static Dictionary<string, string> ValidateDisplayName(string displayName)
    {
        var errors = new Dictionary<string, string>();
        if (DisplayNameLength(displayName))
        {
            errors["displayName"] = "Display name must be between 3 and 64 characters";
            return errors;
        }

        if (!DisplayNameChars(displayName))
        {
            errors["displayName"] = "Display name must only include letters, spaces, hyphens or apostrophes";
        }

        return errors;
    }

    /// <summary>
    /// Runs all password validations and throws relavent excpetions
    /// </summary>
    /// <param name="passwordOne">The ACTUAL password field data</param>
    /// <param name="passwordTwo">The password confirmation field data</param>
    public static Dictionary<string, string> ValidatePassword(string passwordOne, string passwordTwo)
    {
        var errors = new Dictionary<string, string>();

        if (!PasswordMatching(passwordOne, passwordTwo))
        {
            errors["passwordConfirm"] = "Passwords do not match";
        }

        if (!CheckPassword(passwordOne))
        {
            errors["password"] = "Password must be at least 8 characters long including at least one of each uppercase, lowercase, numbers and special characters";
        }

        return errors;
    }

    /// <summary>
    /// Runs all country validations and throws relavent exceptions
    /// </summary>
    /// <param name="country">2-letter country code</param>
    /// <exception cref="InvalidCountryException">Throws if received country ISO is not in the country list.</exception>
    public static void ValidateCountry(string country)
    {
        if (!ValidCountry(country))
        {
            throw new InvalidCountryException(
                "Invalid Country ISO code -- Front End sending wrong country codes"
            );
        }
    }

    /// <summary>
    /// Checks to see if the provided country code is valid or not
    /// </summary>
    /// <param name="country">string representation of a 2-letter country code</param>
    /// <returns>
    /// True: country is in the country code constants list
    /// False: country is not in the country code constants list
    /// </returns>
    public static bool ValidCountry(string country)
    {
        return CountryCodes.All.Contains(country);
    }

    /// <summary>
    /// Checks the password hash and returns a verification result
    /// </summary>
    /// <param name="user">The user you are checking the password for</param>
    /// <param name="password">the password you are checking matches the user</param>
    /// <returns>The verification result</returns>
    public static PasswordVerificationResult VerifyPassword(User user, string password)
    {
        PasswordHasher<User> passwordHasher = new();
        return passwordHasher.VerifyHashedPassword(user, user.PasswordKey, password);
    }

    /// <summary>
    /// Performs all validation for the update password request
    /// </summary>
    /// <param name="user">The users whose password is being updated</param>
    /// <param name="oldPassword">The password that the user wishes to change from</param>
    /// <param name="newPassword">The password the user wishes to change to</param>
    /// <param name="newPasswordConfirm">the new password repeated for confirmation purpses</param>
    /// <returns></returns>
    /// <exception cref="MismatchedPasswordException"></exception>
    /// <exception cref="InvalidPasswordException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static bool ValidateUpdatePasswordRequest(User user, string oldPassword, string newPassword,
        string newPasswordConfirm)
    {
        // Validate old password is the users correct password
        var result = VerifyPassword(user, oldPassword);
        if (!(result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded))
        {
            throw new MismatchedPasswordException("Old password does not match password on file");
        }

        // Validate new passwords match
        if (!PasswordMatching(newPassword, newPasswordConfirm))
        {
            throw new MismatchedPasswordException("Passwords do not match");
        }

        // validate password is of valid form
        if (!CheckPassword(newPassword))
        {
            throw new InvalidPasswordException(
                "Password must be at least 8 characters long including at least one of each uppercase, lowercase, numbers and special characters"
            );
        }

        // Validate new password not the same as old password
        if (PasswordMatching(newPassword, oldPassword))
        {
            throw new ArgumentException("New password can't be the same as old password");
        }

        return true;
    }
}