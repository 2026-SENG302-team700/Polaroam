using System.Text.Json.Serialization;
using SENG302.Api.Helpers;
namespace SENG302.Api.Models.Requests;

public class NewTripRequest
{
    public required string Title { get; set; }

    public required string Description { get; set; }

    [JsonConverter(typeof(NullableDateTimeOffsetConverter))]
    public DateTimeOffset? StartDate { get; set; }

    [JsonConverter(typeof(NullableDateTimeOffsetConverter))]
    public DateTimeOffset? EndDate { get; set; }

}