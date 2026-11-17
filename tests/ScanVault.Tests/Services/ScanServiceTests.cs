using ScanVault.Api.Dtos;
using ScanVault.Api.Errors;
using ScanVault.Api.Models;
using ScanVault.Api.Repositories;
using ScanVault.Api.Services;
using ScanVault.Tests.Helpers;

namespace ScanVault.Tests.Services;

public class ScanServiceTests : IDisposable
{
    private readonly TestDb _testDb = new();
    private readonly InMemoryFileStorage _storage = new();

    private ScanService CreateService()
    {
        var db = _testDb.CreateContext();
        return new ScanService(new ScanRepository(db), new SiteRepository(db), _storage);
    }

    private async Task<int> CreateSiteAsync()
    {
        var service = new SiteService(new SiteRepository(_testDb.CreateContext()), _storage);
        return (await service.CreateAsync(new CreateSiteRequest("Test site", null, null))).Id;
    }

    [Fact]
    public async Task Upload_Glb_StoresFile_AndSavesMetadata()
    {
        var siteId = await CreateSiteAsync();

        var scan = await CreateService().UploadAsync(siteId, TestFiles.Glb("Lobby scan"));

        Assert.Equal(ScanFormat.Model3D, scan.Format);
        Assert.Equal("model/gltf-binary", scan.ContentType);
        Assert.Equal(64, scan.SizeBytes);
        Assert.Equal($"/api/scans/{scan.Id}/file", scan.FileUrl);
        Assert.Single(_storage.Files);
    }

    [Fact]
    public async Task Upload_Png_IsDetectedAsImage()
    {
        var siteId = await CreateSiteAsync();

        var scan = await CreateService().UploadAsync(siteId, TestFiles.Png("Facade photo"));

        Assert.Equal(ScanFormat.Image, scan.Format);
        Assert.Equal("image/png", scan.ContentType);
    }

    [Theory]
    [InlineData("notes.txt")]
    [InlineData("model.obj")]
    [InlineData("noextension")]
    public async Task Upload_UnsupportedType_IsRejected(string fileName)
    {
        var siteId = await CreateSiteAsync();

        await Assert.ThrowsAsync<BadRequestException>(() =>
            CreateService().UploadAsync(siteId, TestFiles.Make(fileName, "Bad file", null)));
        Assert.Empty(_storage.Files);
    }

    [Fact]
    public async Task Upload_EmptyFile_IsRejected()
    {
        var siteId = await CreateSiteAsync();

        await Assert.ThrowsAsync<BadRequestException>(() =>
            CreateService().UploadAsync(siteId, TestFiles.Make("scan.glb", "Empty", null, size: 0)));
    }

    [Fact]
    public async Task Upload_TooLarge_IsRejected_BeforeStoring()
    {
        var siteId = await CreateSiteAsync();
        var upload = TestFiles.Glb("Huge") with { Length = ScanService.MaxFileSizeBytes + 1 };

        await Assert.ThrowsAsync<BadRequestException>(() => CreateService().UploadAsync(siteId, upload));
        Assert.Empty(_storage.Files);
    }

    [Fact]
    public async Task Upload_ToUnknownSite_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateService().UploadAsync(12345, TestFiles.Glb("Orphan")));
    }

    [Fact]
    public async Task GetBySite_ReturnsHistory_NewestFirst()
    {
        var siteId = await CreateSiteAsync();
        var service = CreateService();
        await service.UploadAsync(siteId, TestFiles.Glb("January", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        await service.UploadAsync(siteId, TestFiles.Glb("March", new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)));
        await service.UploadAsync(siteId, TestFiles.Glb("February", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)));

        var history = await CreateService().GetBySiteAsync(siteId);

        Assert.Equal(new[] { "March", "February", "January" }, history.Select(s => s.Title));
    }

    [Fact]
    public async Task OpenFile_ReturnsTheUploadedBytes()
    {
        var siteId = await CreateSiteAsync();
        var scan = await CreateService().UploadAsync(siteId, TestFiles.Glb("Roof"));

        var file = await CreateService().OpenFileAsync(scan.Id);
        using var copy = new MemoryStream();
        await file.Content.CopyToAsync(copy);

        Assert.Equal(64, copy.Length);
        Assert.Equal("model/gltf-binary", file.ContentType);
    }

    [Fact]
    public async Task Delete_RemovesMetadata_AndFile()
    {
        var siteId = await CreateSiteAsync();
        var scan = await CreateService().UploadAsync(siteId, TestFiles.Glb("To delete"));

        await CreateService().DeleteAsync(scan.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().GetAsync(scan.Id));
        Assert.Empty(_storage.Files);
    }

    public void Dispose() => _testDb.Dispose();
}
