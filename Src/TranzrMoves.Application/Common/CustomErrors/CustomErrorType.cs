using System.Net;
using ErrorOr;

namespace TranzrMoves.Application.Common.CustomErrors;

public static class CustomErrorType
{
    public const ErrorType BadRequest = (ErrorType)((int)HttpStatusCode.BadRequest);
    public const ErrorType UnprocessableEntity = (ErrorType)((int)HttpStatusCode.UnprocessableEntity);
    public const ErrorType RemoteFileOperationFailed = (ErrorType)((int)HttpStatusCode.FailedDependency);
    public const ErrorType InternalServerError = (ErrorType)((int)HttpStatusCode.InternalServerError);
    public const ErrorType ServiceUnavailable = (ErrorType)((int)HttpStatusCode.ServiceUnavailable);
    public const ErrorType NotFound = (ErrorType)((int)HttpStatusCode.NotFound);
}