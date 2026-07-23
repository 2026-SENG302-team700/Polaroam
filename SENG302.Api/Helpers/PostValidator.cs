using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Interfaces;

public class PostValidator
{

    /// <summary>
    /// Checks the due date is valid
    /// adds error to dictionary when error occurs.
    /// </summary>
    /// <param name="date"></param>
    public static Dictionary<string, string> ValidatePostDate(DateTime? date)
    {
        var errors = new Dictionary<string, string>();
        // Body removed, awaiting implementation in a different task.
        return errors;
    }

    /// <summary>
    /// given a PostRequest interface and a bool to determine whether profanity is on,
    /// this method invokes validation methods on all post fields to ensure they
    /// meet ACs, and do not contain profanity. Every violation of the post rules is added
    /// to a dictionary, and returned, containing all errors.
    /// </summary>
    /// <param name="post"></param>
    /// <param name="profanityFiltering"></param>
    /// <returns>Dictionary<string, string></returns>
    public static Dictionary<string, string> ValidatePostFields(IPostRequest postRequest, Post? post = null)
    {
        var errors = new Dictionary<string, string>();

        foreach (var (key, value) in ValidatePostTitle(postRequest.Title))
        {
            errors.Add(key, value);
        }

        foreach (var (key, value) in ValidatePostContent(postRequest.Content))
        {
            errors.Add(key, value);
        }
        if (postRequest != post)
        {
            foreach (var (key, value) in ValidatePostDate(postRequest.Date))
            {
                errors.Add(key, value);
            }
        }

        return errors;
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
            errors["name"] = "Title is required and must be between 3 and 128 characters long";
        }
        return errors;
    }

    /// <summary>
    /// Checks the description is valid
    /// adds error to dictionary when error occurs.
    /// </summary>
    /// <param name="description">The description being tested</param>
    public static Dictionary<string, string> ValidatePostContent(string description)
    {
        var errors = new Dictionary<string, string>();
        if (description.Trim().Length > 2048)
        {
            errors["description"] = "Description must be 2048 characters or less";
        }
        return errors;
    }
}