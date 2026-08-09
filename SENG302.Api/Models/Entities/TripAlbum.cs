using System.ComponentModel.DataAnnotations;

namespace SENG302.Api.Models.Entities;

/// <summary>
/// Keeps track of all photos in the trip's album.
/// </summary>
public class TripAlbum
{
    [Key]
    public required int TripId { get; set; }
    public required List<int> PhotoIds { get; set; }
}