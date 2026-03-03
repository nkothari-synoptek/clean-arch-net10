using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using {{ServiceName}}.Application.{{ModuleName}}.Commands.Create{{EntityName}};
using {{ServiceName}}.Application.{{ModuleName}}.Commands.Update{{EntityName}};
using {{ServiceName}}.Application.{{ModuleName}}.Commands.Delete{{EntityName}};
using {{ServiceName}}.Application.{{ModuleName}}.Queries.Get{{EntityName}}ById;
using {{ServiceName}}.Application.{{ModuleName}}.Queries.List{{EntityNamePlural}};

namespace {{ServiceName}}.Api.Controllers.{{ModuleName}};

/// <summary>
/// Controller for managing {{EntityNamePlural}}
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class {{EntityNamePlural}}Controller : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<{{EntityNamePlural}}Controller> _logger;

    public {{EntityNamePlural}}Controller(
        IMediator mediator,
        ILogger<{{EntityNamePlural}}Controller> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new {{EntityName}}
    /// </summary>
    /// <param name="command">The create command</param>
    /// <returns>The ID of the created {{EntityName}}</returns>
    [HttpPost]
    [Authorize(Policy = "CanCreate{{EntityName}}")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] Create{{EntityName}}Command command)
    {
        _logger.LogInformation("Creating new {{EntityName}}");

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to create {{EntityName}}: {Error}", result.Error);
            return BadRequest(new { error = result.Error });
        }

        _logger.LogInformation("Created {{EntityName}} with ID {Id}", result.Value);
        return CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
    }

    /// <summary>
    /// Gets a {{EntityName}} by ID
    /// </summary>
    /// <param name="id">The {{EntityName}} ID</param>
    /// <returns>The {{EntityName}} details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof({{EntityName}}Dto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(Guid id)
    {
        _logger.LogInformation("Getting {{EntityName}} with ID {Id}", id);

        var result = await _mediator.Send(new Get{{EntityName}}ByIdQuery(id));

        if (!result.IsSuccess)
        {
            _logger.LogWarning("{{EntityName}} with ID {Id} not found", id);
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets a paged list of {{EntityNamePlural}}
    /// </summary>
    /// <param name="query">The list query with pagination</param>
    /// <returns>A paged list of {{EntityNamePlural}}</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<{{EntityName}}SummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List([FromQuery] List{{EntityNamePlural}}Query query)
    {
        _logger.LogInformation("Listing {{EntityNamePlural}} (page {PageNumber}, size {PageSize})", 
            query.PageNumber, query.PageSize);

        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to list {{EntityNamePlural}}: {Error}", result.Error);
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Updates an existing {{EntityName}}
    /// </summary>
    /// <param name="id">The {{EntityName}} ID</param>
    /// <param name="command">The update command</param>
    /// <returns>No content on success</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanUpdate{{EntityName}}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(Guid id, [FromBody] Update{{EntityName}}Command command)
    {
        if (id != command.Id)
        {
            _logger.LogWarning("ID mismatch: URL ID {UrlId} vs Command ID {CommandId}", id, command.Id);
            return BadRequest(new { error = "ID mismatch" });
        }

        _logger.LogInformation("Updating {{EntityName}} with ID {Id}", id);

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to update {{EntityName}} with ID {Id}: {Error}", id, result.Error);
            return NotFound(new { error = result.Error });
        }

        _logger.LogInformation("Updated {{EntityName}} with ID {Id}", id);
        return NoContent();
    }

    /// <summary>
    /// Deletes a {{EntityName}}
    /// </summary>
    /// <param name="id">The {{EntityName}} ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CanDelete{{EntityName}}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid id)
    {
        _logger.LogInformation("Deleting {{EntityName}} with ID {Id}", id);

        var result = await _mediator.Send(new Delete{{EntityName}}Command(id));

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Failed to delete {{EntityName}} with ID {Id}: {Error}", id, result.Error);
            return NotFound(new { error = result.Error });
        }

        _logger.LogInformation("Deleted {{EntityName}} with ID {Id}", id);
        return NoContent();
    }
}
