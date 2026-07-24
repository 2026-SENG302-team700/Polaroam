using System.ComponentModel.DataAnnotations;

namespace SENG302.Api.Models.Requests;

/** 
    
*/
public class UpdateTripRequest
{
    public required int Id { get; set; }
    [MaxLength(128)]
    public string? Title { get; set; }

    [MaxLength(2048)]
    public string? Description { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

}