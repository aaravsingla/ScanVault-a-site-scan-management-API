using ScanVault.Api.Dtos;
using ScanVault.Api.Errors;
using ScanVault.Api.Models;
using ScanVault.Api.Repositories;
using ScanVault.Api.Services;
using ScanVault.Tests.Helpers;

namespace ScanVault.Tests.Services;

public class AnnotationServiceTests : IDisposable
{
    private readonly TestDb _testDb = new();
    private readonly InMemoryFileStorage _storage = new();

    private AnnotationService CreateService()
    {
        var db = _testDb.CreateContext();
        return new AnnotationService(new AnnotationRepository(db), new ScanRepository(db));
    }

    private async Task<ScanDto> CreateScanAsync(ScanUpload upload)
    {
        var db = _testDb.CreateContext();
        var site = await new SiteService(new SiteRepository(db), _storage)
            .CreateAsync(new CreateSiteRequest("Site", null, null));
        return await new ScanService(new ScanRepository(db), new SiteRepository(db), _storage)
            .UploadAsync(site.Id, upload);
    }

    [Fact]
    public async Task Create_On3DModel_StoresPosition()
    {
        var scan = await CreateScanAsync(TestFiles.Glb("Model"));

        var note = await CreateService().CreateAsync(scan.Id,
            new CreateAnnotationRequest("Crack near window", AnnotationCategory.Issue, 1.5, -0.2, 3.0));

        Assert.Equal(AnnotationCategory.Issue, note.Category);
        Assert.Equal((1.5, -0.2, 3.0), (note.X, note.Y, note.Z));
    }

    [Fact]
    public async Task Create_OnImage_RequiresNormalisedCoordinates()
    {
        var scan = await CreateScanAsync(TestFiles.Png("Photo"));

        await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateAsync(scan.Id,
            new CreateAnnotationRequest("Outside the photo", AnnotationCategory.Note, 1.4, 0.5, 0)));

        var ok = await CreateService().CreateAsync(scan.Id,
            new CreateAnnotationRequest("Inside the photo", AnnotationCategory.Note, 0.25, 0.75, 0));
        Assert.True(ok.Id > 0);
    }

    [Fact]
    public async Task Create_OnUnknownScan_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().CreateAsync(404,
            new CreateAnnotationRequest("Lost", AnnotationCategory.Note, 0, 0, 0)));
    }

    [Fact]
    public async Task GetByScan_ReturnsAnnotations_AndDeleteRemovesThem()
    {
        var scan = await CreateScanAsync(TestFiles.Glb("Model"));
        var first = await CreateService().CreateAsync(scan.Id, new CreateAnnotationRequest("A", AnnotationCategory.Note, 0, 0, 0));
        await CreateService().CreateAsync(scan.Id, new CreateAnnotationRequest("B", AnnotationCategory.Progress, 1, 1, 1));

        Assert.Equal(2, (await CreateService().GetByScanAsync(scan.Id)).Count);

        await CreateService().DeleteAsync(first.Id);

        var remaining = Assert.Single(await CreateService().GetByScanAsync(scan.Id));
        Assert.Equal("B", remaining.Text);
    }

    public void Dispose() => _testDb.Dispose();
}
