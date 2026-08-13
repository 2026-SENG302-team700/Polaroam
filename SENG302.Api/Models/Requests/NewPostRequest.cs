using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SENG302.Api.Models.Interfaces;
namespace SENG302.Api.Models.Entities;

/// <summary>
/// Represents a request by a user to create a post.
/// </summary>
public class NewPostRequest : IPostRequest
{
    public required int TripId { get; set; }
    public required string Title { get; set; }
    public required long OsmId { get; set; }
    public required string OsmType { get; set; }

    [Required(AllowEmptyStrings = true)]
    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public required string Content { get; set; }
    public DateOnly? Date
    {
        get;
        set
        {
            field = value;
            DateSupplied = true;
        }
    }
    [JsonIgnore]
    public bool DateSupplied { get; set; } = false;
    public bool NoContentAllowed { get; set; } = false;
}


