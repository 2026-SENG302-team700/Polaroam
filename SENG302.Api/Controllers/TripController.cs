using Microsoft.AspNetCore.Mvc;
using SENG302.Api.Services;
using SENG302.Api.Models.Entities;
using SENG302.Api.Models.Requests;
using SENG302.Api.Filters;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
namespace SENG302.Api.Controllers;

[ConditionalValidateAntiForgeryToken]
[Authorize]
[ApiController]
[Route("api/trip")]
public class TripController : ControllerBase
{
    private readonly ITripService _tripService;

    public TripController(ITripService tripService)
    {
        _tripService = tripService;
    }

    /// <summary>
    /// Gets a trip by its ID. Returns 404 if no trip with the given ID exists.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Trip>> getTrip(int id)
    {
        // get the user id
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var trip = await _tripService.GetTripByIdAsync(id, int.Parse(userIdString));
        if (trip == null)
        {
            return NotFound();
        }
        return Ok(trip);
    }

    /// <summary>
    /// Gets all trips associated with a user's email. Returns an empty array if no trips are found for the given email.
    /// </summary>
    /// <param name="userEmail"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Trip>>> GetTripsForUser()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
            return Unauthorized();


        var trips = await _tripService.GetTripsByUserIdAsync(int.Parse(userIdString));
        return Ok(trips);
    }

    /// <summary>
    /// Creates a new trip for a user with the given email and name. 
    /// Validates the name and user email before creating the trip.
    /// Returns 400 if validation fails, or 200 if the trip is created successfully.
    /// </summary>
    /// <param name="tripRequest"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<ActionResult<Trip>> CreateTrip([FromBody] NewTripRequest tripRequest)
    {
        try
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized();
            var trip = await _tripService.CreateNewTripAsync(tripRequest, int.Parse(userIdString));
            return Ok(trip);
        }
        catch (ArgumentException e)
        {
            return BadRequest(e.Message);
        }
    }
}