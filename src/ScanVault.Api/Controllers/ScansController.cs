using Microsoft.AspNetCore.Mvc;
using ScanVault.Api.Dtos;
using ScanVault.Api.Services;

namespace ScanVault.Api.Controllers;

/// <summary>REST endpoints for one scan and its annotations: /api/scans</summary>
[ApiController]
[Route("api/scans")]
[Produces("application/json")]
public class ScansController : ControllerBase
{
    private readonly IScanService _scans;
    private readonly IAnnotationService _annotations;

    public ScansController(IScanService scans, IAnnotationService annotations)
    {
        _scans = scans;
        _annotations = annotations;
    }

    /// <summary>Gets one scan's metadata.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScanDto>> Get(int id) => Ok(await _scans.GetAsync(id));

    /// <summary>Streams the scan file itself (from Blob Storage in the cloud).</summary>
    [HttpGet("{id:int}/file")]
    [Produces("application/octet-stream")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFile(int id)
    {
        var file = await _scans.OpenFileAsync(id);
        return File(file.Content, file.ContentType, enableRangeProcessing: true);
    }

    /// <summary>Deletes a scan, its annotations and its stored file.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id)
    {
        await _scans.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>Lists the annotations pinned on a scan.</summary>
    [HttpGet("{scanId:int}/annotations")]
    public async Task<ActionResult<List<AnnotationDto>>> GetAnnotations(int scanId) =>
        Ok(await _annotations.GetByScanAsync(scanId));

    /// <summary>Pins a new annotation at an X/Y/Z position on a scan.</summary>
    [HttpPost("{scanId:int}/annotations")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AnnotationDto>> CreateAnnotation(int scanId, CreateAnnotationRequest request)
    {
        var annotation = await _annotations.CreateAsync(scanId, request);
        return Created($"/api/scans/{scanId}/annotations/{annotation.Id}", annotation);
    }
}
