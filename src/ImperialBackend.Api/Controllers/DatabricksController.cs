using ImperialBackend.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImperialBackend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DatabricksController : ControllerBase
{
    private readonly IDatabricksQueryService _queryService;

    public DatabricksController(IDatabricksQueryService queryService)
    {
        _queryService = queryService;
    }

    [HttpPost("query")] 
    [ProducesResponseType(typeof(IEnumerable<IDictionary<string, object?>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Query([FromBody] GenericQuerySpec spec, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(spec.Catalog) || string.IsNullOrWhiteSpace(spec.Schema) || string.IsNullOrWhiteSpace(spec.Table))
        {
            return BadRequest("Catalog, Schema and Table are required");
        }

        var rows = await _queryService.QueryAsync(spec, cancellationToken);
        return Ok(rows);
    }
}