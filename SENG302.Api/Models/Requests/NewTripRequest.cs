using System.ComponentModel.DataAnnotations;

namespace SENG302.Api.Models.Requests;

/** 
    
*/
public class NewTripRequest
{

    public required int UserId { get; set; }

    [MaxLength(128)]
    public required string Title { get; set; }

    [MaxLength(2048)]
    public string Description { get; set; }

    public required DateTime? StartDate { get; set; }
    public required DateTime? EndDate { get; set; }

}