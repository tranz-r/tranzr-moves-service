using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TranzrMoves.Api.Common.Http;
using TranzrMoves.Application.Common.CustomErrors;

namespace TranzrMoves.Api.Controllers;

public class ApiControllerBase : ControllerBase
{
    internal const string ApplicationJson = "application/json";

    internal const string CookieName = "tranzr_guest";
    protected IActionResult Problem(List<Error> errors)
    {
        if (errors.Count is 0)
        {
            return Problem();
        }

        if (errors.All(error => error.Type == ErrorType.Validation))
        {
            return ValidationProblem(errors);
        }

        HttpContext.Items[HttpContextItemKeys.Errors] = errors;
        
        return Problem(errors.FirstOrDefault());
    }

    private IActionResult Problem(Error error)
    {
        var statusCode = error.Type switch
        {
            CustomErrorType.BadRequest => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            CustomErrorType.ServiceUnavailable => StatusCodes.Status503ServiceUnavailable,
            CustomErrorType.RemoteFileOperationFailed => StatusCodes.Status424FailedDependency,
            _ => StatusCodes.Status500InternalServerError
        };

        return Problem(statusCode: statusCode, title: error.Description);
    }

    private IActionResult ValidationProblem(List<Error> errors)
    {
        var modelState = new ModelStateDictionary();
        foreach (var error in errors)
        {
            modelState.AddModelError(error.Code, error.Description);
        }

        return ValidationProblem(modelState);
    }
}