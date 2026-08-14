using System.Globalization;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Interfaces;

public class PostValidator
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
    /// Checks the due date is valid
    /// adds error to dictionary when error occurs.
    /// </summary>
    /// <param name="date"></param>
    public static List<string> ValidatePostDate(DateOnly? date)
    {
        var errors = new List<string>();
        if (date == null)
        {
            errors.Add($"Invalid date, date must be formatted {GetLocaleDateFormat(CultureInfo.CurrentCulture)}");
            return errors;
        }

        if (date > DateOnly.FromDateTime(DateTime.Today)) errors.Add("Invalid date, date cannot be in the future");
        return errors;
    }

    /// <summary>
    /// given a PostRequest interface,
    /// this method invokes validation methods on all post fields to ensure they
    /// meet ACs. Every violation of the post rules is added
    /// to a dictionary, and returned, containing all errors.
    /// </summary>
    /// <param name="post"></param>
    /// <returns>Dictionary<string, string></returns>
    public static Dictionary<string, List<string>> ValidatePostFields(IPostRequest postRequest, Post? post = null)
    {
        var errors = new Dictionary<string, List<string>>();

        errors["title"] = ValidatePostTitle(postRequest.Title)
            .Values
            .ToList();

        errors["date"] = ValidatePostDate(postRequest.Date);

        errors["location"] = ValidatePostLocation(postRequest.OsmId, postRequest.OsmType);

        errors["content"] = ValidatePostContent(postRequest.Content, postRequest.NoContentAllowed);

        return errors;
    }

    /// <summary>
    /// Checks the post location is valid, returns nothing if valid
    /// </summary>
    /// <param name="osmId"></param>
    /// <returns></returns>
    public static List<string> ValidatePostLocation(long osmId, string osmType)
    {
        if (osmId <= 0 || string.IsNullOrWhiteSpace(osmType) || !new[] { "node", "way", "relation" }.Contains(osmType.ToLower()))
        {
            return ["This isn't a valid location"];
        }

        return [];
    }


    /// <summary>
    /// Checks the post name is valid, returns nothing if valid
    /// adds error to dictionary when error occurs.
    /// </summary>
    /// <param name="title">The name being tested</param>
    public static Dictionary<string, string> ValidatePostTitle(string title)
    {
        var errors = new Dictionary<string, string>();
        var postTitle = title.Trim();
        if (postTitle.Length < 3 || postTitle.Length > 128)
        {
            errors["title"] = "Title is required and must be between 3 and 128 characters long";
        }
        return errors;
    }

    /// <summary>
    /// Checks for no content
    /// if no content exists and no content isn't allowed, return error
    /// </summary>
    /// <param name="content">the post content</param>
    /// <param name="noContentAllowed">tag of whether the post can be created without content</param>
    /// <returns>The errors</returns>
    public static List<string> ValidatePostContent(string content, bool noContentAllowed)
    {
        var errors = new List<string>();

        if (content.Length == 0 && (!noContentAllowed))
        {
            errors.Add("Post contains no content, are you sure you want to create post?");
        }

        if (content.Length > 8196)
        {
            errors.Add("Content needs to be less than 8196 characters long");
        }

        return errors;
    }
}