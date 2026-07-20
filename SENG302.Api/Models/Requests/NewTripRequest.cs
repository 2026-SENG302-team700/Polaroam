using System.ComponentModel.DataAnnotations;

namespace SENG302.Api.Models.Requests;

/** 
    
*/
public class NewTripRequest
{
    [MaxLength(128)]
    public required string Title { get; set; }

    [MaxLength(2048)]
    public required string Description { get; set; }

    public required DateTime? StartDate { get; set; }
    public required DateTime? EndDate { get; set; }

}