using SENG302.Api.Services.Email;

namespace SENG302.Api.Models.Jobs;

/// <summary>
/// Represents a job to send an email. Contains the recipient email address, the template to use
/// and a dictionary of model data to populate the template.
/// </summary>
/// <param name="toEmail">Email to send to</param>
/// <param name="Template">The email template to use</param>
/// <param name="Model">The model data to populate the template</param>
public record EmailJob(
    string toEmail,
    EmailTemplate Template,
    Dictionary<string, string> Model
);