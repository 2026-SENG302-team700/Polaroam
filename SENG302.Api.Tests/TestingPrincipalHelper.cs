using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SENG302.Api.Tests;

static public class TestingPrincipalHelper
{

    /// <summary>
    /// Adds a generic principal to a controller for testing purposes
    /// The data that is added is:
    ///     Id: 1
    ///     Name: John Example
    ///     Email: john@example.com
    /// </summary>
    /// <param name="controller">The controller we are adding the principal to</param>
    static public void AddTestPrincipalToController(ControllerBase controller)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = CreateTestUserPrincipal()
            }
        };
    }

    /// <summary>
    /// Adds a generic principal with a garbled id to a controller for testing purposes
    /// The data that is added is:
    ///     Id: 'garbled' (will cause int.Parse to fail)
    ///     Name: Jacob Example
    ///     Email: jacob@example.com
    /// </summary>
    /// <param name="controller">The controller we are adding the principal to</param>
    static public void AddGarbledPrincipalToController(ControllerBase controller)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = CreateGarbledIdUserPrincipal()
            }
        };
    }

    /// <summary>
    /// Removes the principal from a controller
    /// Use for tests where the user is not logged in
    /// </summary>
    /// <param name="controller">The controller we are removing the principal from</param>
    static public void RemoveTestPrincipalFromController(ControllerBase controller)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    /// <summary>
    /// Creates a test user principal with the following data:
    ///     Id: 1
    ///     Name: John Example
    ///     Email: john@example.com
    /// </summary>
    /// <returns>A generic user principal for testing purposes</returns>
    /// 
    static public ClaimsPrincipal CreateTestUserPrincipal()
    {
        return CreateUserPrincipal("1", "John Example", "john@example.com");
    }

    /// <summary>
    /// Creates a test user principal with a garbled id
    /// It has the following data:
    ///     Id: 'garbled' (will cause int.Parse to fail)
    ///     Name: Jacob Example
    ///     Email: jacob@example.com
    /// </summary>
    /// <returns>A generic user principal for testing purposes, but with a garbled id</returns>
    static public ClaimsPrincipal CreateGarbledIdUserPrincipal()
    {
        return CreateUserPrincipal("garbled", "Jacob Example", "jacob@example.com");
    }

    /// <summary>
    /// Creates a user principal based on a userId, name, and email
    /// </summary>
    /// <param name="userId">The userId claimed by the principal</param>
    /// <param name="name">The name claimed by the principal</param>
    /// <param name="email">The email claimed by the principal</param>
    /// <returns>A ClaimsPrincipal with the data given</returns>
    static public ClaimsPrincipal CreateUserPrincipal(string userId, string name, string email)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId), new Claim(ClaimTypes.Name, name), new Claim(ClaimTypes.Email, email) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        return principal;
    }
}

