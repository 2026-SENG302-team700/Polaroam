using System.ComponentModel.DataAnnotations;

namespace SENG302.Api.Models.Requests;

/** 
    
*/
public class UpdateTripRequest
{
    public required int Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

}