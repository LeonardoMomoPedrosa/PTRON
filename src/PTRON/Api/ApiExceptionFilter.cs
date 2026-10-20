using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PTRON.Services;

namespace PTRON.Api;

public sealed class ApiExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not InvalidOperationException ex)
        {
            return;
        }

        var status = ex is NotFoundException
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status400BadRequest;

        context.Result = new ObjectResult(new ErrorDto { Error = ex.Message })
        {
            StatusCode = status
        };
        context.ExceptionHandled = true;
    }
}
