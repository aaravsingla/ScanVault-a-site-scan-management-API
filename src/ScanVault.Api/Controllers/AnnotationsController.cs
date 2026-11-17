using Microsoft.AspNetCore.Mvc;
using ScanVault.Api.Services;

namespace ScanVault.Api.Controllers;

/// <summary>REST endpoint for removing a single annotation: /api/annotations/{id}</summary>
[ApiController]
[Route("api/annotations")]
public class AnnotationsController : ControllerBase
{
    private readonly IAnnotationService _annotations;

    public AnnotationsController(IAnnotationService annotations)
    {
        _annotations = annotations;
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await _annotations.DeleteAsync(id);
        return NoContent();
    }
}
