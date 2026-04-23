using HospitalNoShow.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace HospitalNoShow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    protected IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess) return Ok(result.Value);

        return result.Errors.Count > 1
            ? BadRequest(new { errors = result.Errors })
            : BadRequest(new { error = result.Error });
    }

    protected IActionResult ToCreatedResult<T>(Result<T> result, string actionName, object? routeValues = null)
    {
        if (result.IsSuccess)
            return CreatedAtAction(actionName, routeValues, result.Value);

        return result.Errors.Count > 1
            ? BadRequest(new { errors = result.Errors })
            : BadRequest(new { error = result.Error });
    }

    protected IActionResult ToActionResult(Result result)
    {
        if (result.IsSuccess) return NoContent();

        return result.Errors.Count > 1
            ? BadRequest(new { errors = result.Errors })
            : BadRequest(new { error = result.Error });
    }
}
