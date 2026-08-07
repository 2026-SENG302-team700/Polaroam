using System.ComponentModel.DataAnnotations;

namespace SENG302.Api.Models.Requests;

public class NewTripRequest
{
    public required string Title { get; set; }

    public required string Description { get; set; }

    public required DateTimeOffset? StartDate { get; set; }
    public required DateTimeOffset? EndDate { get; set; }

}