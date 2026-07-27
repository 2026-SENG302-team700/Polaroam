using System.Globalization;
using System.Net.NetworkInformation;
using SENG302.Api.Services;

/// <summary>
/// Class for validating trip data
/// </summary>
public class TripValidator
{
    /// <summary>
    /// Returns a string representing the date format of the users locale
    /// </summary>
    /// <param name="culture"></param>
    /// <returns>dd/MM/yyyy or similar</returns>
    public static string GetLocaleDateFormat(CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        string pattern = culture.DateTimeFormat.ShortDatePattern;
        return pattern;
    }

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

        if (!ValidTitleLength(title) || title.IsWhiteSpace())
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
    public static Dictionary<string, List<string>> ValidateDates(DateTime? startDate, DateTime? endDate)
    {
        var errors = new Dictionary<string, List<string>>();
        errors["dates"] = new List<string>();

        if (startDate > endDate)
        {
            errors["dates"].Add("End date must not be before the start date");
        }

        if (startDate == null || endDate == null)
        {
            errors["dates"].Add($"Invalid date, date must be formatted {GetLocaleDateFormat()}");
        }

        return errors;
    }

}
