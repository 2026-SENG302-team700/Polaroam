using SENG302.Api.Models.Entities;

namespace SENG302.Api.Models.Jobs;

public record ImageTaggingJob(IFormFile Image, int ImageId);