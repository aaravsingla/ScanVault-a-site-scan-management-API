using ScanVault.Api.Dtos;
using ScanVault.Api.Errors;
using ScanVault.Api.Repositories;
using ScanVault.Api.Services;
using ScanVault.Tests.Helpers;

namespace ScanVault.Tests.Services;

public class SiteServiceTests : IDisposable
{
    private readonly TestDb _testDb = new();
    private readonly InMemoryFileStorage _storage = new();

    private SiteService CreateSiteService() => new(new SiteRepository(_testDb.CreateContext()), _storage);

    private ScanService CreateScanService()
    {
        var db = _testDb.CreateContext();
        return new ScanService(new ScanRepository(db), new SiteRepository(db), _storage);
    }

    [Fact]
    public async Task Create_TrimsInput_AndReturnsSite()
    {
        var created = await CreateSiteService().CreateAsync(
            new CreateSiteRequest("  Block A - Floor 2 ", " Hyderabad ", null));

        Assert.True(created.Id > 0);
        Assert.Equal("Block A - Floor 2", created.Name);
        Assert.Equal("Hyderabad", created.Location);
        Assert.Empty(created.Scans);
    }

    [Fact]
    public async Task GetAll_ReturnsScanCountAndLatestCapture()
    {
        var site = await CreateSiteService().CreateAsync(new CreateSiteRequest("Warehouse", null, null));
        var scans = CreateScanService();
        await scans.UploadAsync(site.Id, TestFiles.Glb("Week 1", new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)));
        await scans.UploadAsync(site.Id, TestFiles.Glb("Week 2", new DateTime(2026, 1, 12, 0, 0, 0, DateTimeKind.Utc)));

        var summary = Assert.Single(await CreateSiteService().GetAllAsync());

        Assert.Equal(2, summary.ScanCount);
        Assert.Equal(new DateTime(2026, 1, 12, 0, 0, 0, DateTimeKind.Utc), summary.LastCapturedAt);
    }

    [Fact]
    public async Task Get_UnknownSite_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => CreateSiteService().GetAsync(999));
    }

    [Fact]
    public async Task Update_ChangesFields()
    {
        var site = await CreateSiteService().CreateAsync(new CreateSiteRequest("Old name", null, null));

        var updated = await CreateSiteService().UpdateAsync(site.Id, new UpdateSiteRequest("New name", "Pune", "Phase 2"));

        Assert.Equal("New name", updated.Name);
        Assert.Equal("Pune", updated.Location);
        Assert.Equal("Phase 2", updated.Description);
    }

    [Fact]
    public async Task Delete_RemovesSite_ItsScans_AndTheirFiles()
    {
        var site = await CreateSiteService().CreateAsync(new CreateSiteRequest("Temporary site", null, null));
        await CreateScanService().UploadAsync(site.Id, TestFiles.Glb("Scan"));
        Assert.Single(_storage.Files);

        await CreateSiteService().DeleteAsync(site.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateSiteService().GetAsync(site.Id));
        Assert.Empty(_storage.Files);
    }

    public void Dispose() => _testDb.Dispose();
}
