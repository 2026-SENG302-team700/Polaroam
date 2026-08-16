using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SENG302.Api.Models.Requests;


public class UpdateTripRequest
{
    public required int Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }

    public DateTimeOffset? StartDate { get;
        set {
            field = value;
            StartDateSupplied = true;
        }
    }
    [JsonIgnore]
    public bool StartDateSupplied { get; set; } = false;


    public DateTimeOffset? EndDate { get;
        set {
            field = value;
            EndDateSupplied = true;
        }
    }
    [JsonIgnore]
    public bool EndDateSupplied { get; set; } = false;

}