using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using ScanVault.Api.Dtos;

namespace ScanVault.Tests.Api;

/// <summary>
/// Starts the whole API in memory and calls it over HTTP, checking routes, status codes,
/// validation and the multipart upload, end to end. Uses a temporary SQLite file and folder.
/// </summary>
public class ApiTests : IClassFixture<ApiTests.ScanVaultApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public ApiTests(ScanVaultApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task FullFlow_CreateSite_UploadScan_Annotate_Download()
    {
        // Create a site
        var siteResponse = await _client.PostAsJsonAsync("/api/sites", new { name = "API test site", location = "Hyderabad" });
        Assert.Equal(HttpStatusCode.Created, siteResponse.StatusCode);
        var site = (await siteResponse.Content.ReadFromJsonAsync<SiteDetailDto>(Json))!;

        // Upload a scan as multipart/form-data, like the browser does
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("model/gltf-binary");
        form.Add(file, "File", "lobby.glb");
        form.Add(new StringContent("Lobby scan"), "Title");

        var uploadResponse = await _client.PostAsync($"/api/sites/{site.Id}/scans", form);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);
        var scan = (await uploadResponse.Content.ReadFromJsonAsync<ScanDto>(Json))!;

        // Pin an annotation
        var noteResponse = await _client.PostAsJsonAsync($"/api/scans/{scan.Id}/annotations",
            new { text = "Check this beam", category = "Issue", x = 0.5, y = 1.2, z = -0.3 });
        Assert.Equal(HttpStatusCode.Created, noteResponse.StatusCode);

        // The site now shows the scan in its history
        var detail = (await _client.GetFromJsonAsync<SiteDetailDto>($"/api/sites/{site.Id}", Json))!;
        var listed = Assert.Single(detail.Scans);
        Assert.Equal(1, listed.AnnotationCount);

        // The file downloads unchanged
        var downloaded = await _client.GetByteArrayAsync(scan.FileUrl);
        Assert.Equal(bytes, downloaded);
    }

    [Fact]
    public async Task CreateSite_WithoutName_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/sites", new { location = "Nowhere" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UnknownSite_Returns404_AsProblemDetails()
    {
        var response = await _client.GetAsync("/api/sites/987654");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Site 987654 was not found.", body);
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var body = await _client.GetStringAsync("/health");
        Assert.Equal("Healthy", body);
    }

    public class ScanVaultApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "scanvault-tests-" + Guid.NewGuid().ToString("N"));

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Database:Provider", "Sqlite");
            builder.UseSetting("ConnectionStrings:ScanVault", $"Data Source={Path.Combine(_folder, "test.db")}");
            builder.UseSetting("Storage:Provider", "Local");
            builder.UseSetting("Storage:Local:Path", Path.Combine(_folder, "files"));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try { Directory.Delete(_folder, recursive: true); } catch (IOException) { }
        }
    }
}
