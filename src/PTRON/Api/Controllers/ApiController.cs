using Microsoft.AspNetCore.Mvc;

namespace PTRON.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class ApiRootController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiInfoDto> Get() => Ok(new ApiInfoDto());
}
