using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SENG302.Api.Models.Entities;

namespace SENG302.Api.Models.Interfaces;

public interface IPostRequest {
    public  string Title { get; set; }
    public  string Content { get; set; }
    public  DateTimeOffset? Date { get; set; }
    [JsonIgnore]
    public bool DateSupplied {get; set;}
    public long OsmId { get; set; }
    public bool NoContentAllowed { get; set; }
}