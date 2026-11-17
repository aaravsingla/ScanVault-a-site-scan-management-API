using Microsoft.AspNetCore.Mvc;
using ScanVault.Api.Dtos;
using ScanVault.Api.Services;

namespace ScanVault.Api.Controllers;

/// <summary>REST endpoints for sites: /api/sites</summary>
[ApiController]
[Route("api/sites")]
[Produces("application/json")]
public class SitesController : ControllerBase
{
    private readonly ISiteService _sites;
    private readonly IScanService _scans;

    // Dependency injection: ASP.NET Core creates the services and passes them in.
    public SitesController(ISiteService sites, IScanService scans)
    {
        _sites = sites;
        _scans = scans;
    }

    /// <summary>Lists all sites with their scan count and latest capture date.</summary>
    [HttpGet]
    public async Task<ActionResult<List<SiteSummaryDto>>> GetAll() => Ok(await _sites.GetAllAsync());

    /// <summary>Gets one site with its scan history (newest first).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SiteDetailDto>> Get(int id) => Ok(await _sites.GetAsync(id));

    /// <summary>Creates a site.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SiteDetailDto>> Create(CreateSiteRequest request)
    {
        var site = await _sites.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = site.Id }, site);
    }

    /// <summary>Updates a site's name, location and description.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SiteDetailDto>> Update(int id, UpdateSiteRequest request) =>
        Ok(await _sites.UpdateAsync(id, request));

    /// <summary>Deletes a site, its scans, their annotations and stored files.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id)
    {
        await _sites.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>Scan history of a site, newest capture first.</summary>
    [HttpGet("{siteId:int}/scans")]
    public async Task<ActionResult<List<ScanDto>>> GetScans(int siteId) =>
        Ok(await _scans.GetBySiteAsync(siteId));

    /// <summary>Uploads a scan file (.glb, .jpg or .png, up to 50 MB) to a site.</summary>
    [HttpPost("{siteId:int}/scans")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(55 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 55 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ScanDto>> UploadScan(int siteId, [FromForm] UploadScanRequest request)
    {
        await using var stream = request.File.OpenReadStream();
        var scan = await _scans.UploadAsync(siteId, new ScanUpload(
            stream, request.File.FileName, request.File.Length,
            request.Title, request.Notes, request.CapturedAt));

        return CreatedAtAction(nameof(ScansController.Get), "Scans", new { id = scan.Id }, scan);
    }
}
