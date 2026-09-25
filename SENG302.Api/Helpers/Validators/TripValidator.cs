using System.Globalization;

namespace SENG302.Api.Helpers.Validators;
/// <summary>
/// Class for validating trip data
/// </summary>
public static class TripValidator
{

    /// <summary>
    /// Checks to see if Title  Length is between 3-64 characters
    /// </summary>
    /// <param name="title">String representing the trip title</param>
    /// <returns>
    /// True: If title is between 3-128 characters
    /// False: If title is less than 3 characters or greater than 128 characters
    /// </returns>
    public static bool ValidTitleLength(string title)
    {
        return !((title.Length < 3) || (title.Length > 128));
    }

    /// <summary>
    /// Checks to see if the title characters are valid
    /// </summary>
    /// <param name="title">String representing the trips title</param>
    /// <returns>False if contains chars other than letters, spaces, hyphens, apostrophes, or numbers
    /// True otherwise</returns>
    public static bool ValidTitleChars(string title)
    {
        return ValidationPatterns.TripTitle.IsMatch(title);
    }

    /// <summary>
    /// Validates the trip title by checking length and characters
    /// </summary>
    /// <param name="title"></param>
    /// <returns>A dictionary of fields and lists of their errors</returns>
    public static Dictionary<string, List<string>> ValidateTitle(string title)
    {
        var errors = new Dictionary<string, List<string>>();
        errors["title"] = new List<string>();

        if (title.IsWhiteSpace())
        {
            errors["title"].Add("Trip title is required and must be between 3 and 128 characters long");
            return errors;
        }

        if (!ValidTitleLength(title))
        {
            errors["title"].Add("Trip title is required and must be between 3 and 128 characters long");
        }

        if (!ValidTitleChars(title))
        {
            errors["title"].Add("Trip title cannot contain characters other than letters, spaces, hyphens, apostrophes, or numbers");
        }
        return errors;
    }

    /// <summary>
    /// Validates the trip title by checking length
    /// </summary>
    /// <param name="description"></param>
    /// <returns>A dictionary of fields and lists of their errors</returns>
    public static Dictionary<string, List<string>> ValidateDescription(string description)
    {
        var errors = new Dictionary<string, List<string>>();
        errors["description"] = new List<string>();

        if (description.Length > 2048)
        {
            errors["description"].Add("Description must be 2048 characters or less");
        }
        return errors;
    }

    /// <summary>
    /// Validates that the start date is before the end dates and that the dates are not null
    /// </summary>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <returns></returns>
    public static Dictionary<string, List<string>> ValidateDates(DateTimeOffset? startDate, DateTimeOffset? endDate)
    {
        var errors = new Dictionary<string, List<string>>();
        errors["dateOne"] = new List<string>();
        errors["dateTwo"] = new List<string>();

        if (startDate > endDate)
        {
            errors["dateTwo"].Add("End date must not be before the start date");
        }

        if (startDate == null)
        {
            errors["dateOne"].Add("Date is invalid");
        }

        if (endDate == null)
        {
            errors["dateTwo"].Add("Date is invalid");
        }

        if (startDate < new DateTimeOffset(new DateOnly(1900, 1, 1), TimeOnly.MinValue, TimeSpan.Zero))
        {
            errors["dateOne"].Add("Invalid, date must not be before January 1st 1900");
        }

        if (endDate < new DateTimeOffset(new DateOnly(1900, 1, 1), TimeOnly.MinValue, TimeSpan.Zero))
        {
            errors["dateTwo"].Add("Invalid, date must not be before January 1st 1900");
        }

        return errors;
    }

}
