using System.Text.Json.Serialization;
using SENG302.Api.Models.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace SENG302.Api.Models.Entities;

/// <summary>
/// Represents updates to a pre-existing post.
/// PostId needs to be present, everything else optional.
/// </summary>
public class UpdatePostRequest : IPostRequest
{
    public required int Id { get; set; }
    public required int TripId { get; set; }
    public required string Title { get; set; }
    public long OsmId { get; set; }
    public string OsmType { get; set; } = "node";
    [Required(AllowEmptyStrings = true)]
    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string Content { get; set; } = string.Empty;

    public required DateTimeOffset? Date
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
