using SENG302.Api.Services;

namespace SENG302.Api.Models.Jobs;

public record EmailJob(
    string toEmail,
    EmailTemplate Template,
    Dictionary<string, string> Model
);