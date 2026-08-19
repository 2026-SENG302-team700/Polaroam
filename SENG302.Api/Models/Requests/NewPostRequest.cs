using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using SENG302.Api.Helpers;
using SENG302.Api.Models.Interfaces;
namespace SENG302.Api.Models.Entities;

/// <summary>
/// Represents a request by a user to create a post.
/// </summary>
public class NewPostRequest : IPostRequest
{
    public required int TripId { get; set; }
    [Required(AllowEmptyStrings = true)]
    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public required string Title { get; set; }
    public required long OsmId { get; set; }
    public string? OsmType { get; set; } = "node";

    [Required(AllowEmptyStrings = true)]
    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public required string Content { get; set; }

    [ModelBinder(typeof(NullableDateTimeOffsetModelBinder))]
    public DateTimeOffset? Date
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

