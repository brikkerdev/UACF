using UACF.Models;

namespace UACF.Core
{
    public static class ResponseHelper
    {
        public static void Ok(RequestContext ctx, object data, long durationMs = 0)
        {
            ctx.RespondOk(data, durationMs);
        }

        public static void Error(RequestContext ctx, int statusCode, ErrorCode code, string message, object details = null, long durationMs = 0)
        {
            ctx.RespondError(statusCode, code, message, details, durationMs);
        }

        public static void InvalidRequest(RequestContext ctx, string message, object details = null)
        {
            Error(ctx, 400, ErrorCode.INVALID_REQUEST, message, details);
        }

        public static void NotFound(RequestContext ctx, string message)
        {
            Error(ctx, 404, ErrorCode.NOT_FOUND, message);
        }

        public static void InternalError(RequestContext ctx, string message, object details = null)
        {
            Error(ctx, 500, ErrorCode.INTERNAL_ERROR, message, details);
        }

        public static void ServerBusy(RequestContext ctx, string message = "Server is compiling", int retryAfterSeconds = 5)
        {
            ctx.SetResponseHeader("Retry-After", retryAfterSeconds.ToString());
            Error(ctx, 503, ErrorCode.SERVER_BUSY, message, null, 0);
        }

        public static void Conflict(RequestContext ctx, string message)
        {
            Error(ctx, 409, ErrorCode.CONFLICT, message);
        }

        public static void Unprocessable(RequestContext ctx, ErrorCode code, string message, object details = null)
        {
            Error(ctx, 422, code, message, details);
        }
    }
}
