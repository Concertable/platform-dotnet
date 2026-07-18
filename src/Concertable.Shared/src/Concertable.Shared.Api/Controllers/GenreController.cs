using Concertable.Contracts.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.Shared.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class GenreController : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<Genre>> GetAll() => Ok(Enum.GetValues<Genre>());
}
