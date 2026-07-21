using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FluentAssertions;
using LitigApp.Api.IntegrationTests.Common;
using LitigApp.Domain.Imports;
using LitigApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LitigApp.Api.IntegrationTests.Features.Imports;

/// <summary>
/// GET /api/v1/imports/{id}/errors.csv — same ImportErrorsCsvBuilder as the ImportComplete
/// email attachment, so the CSV must be byte-for-byte identical (blueprint §9).
/// </summary>
public sealed class ImportErrorsCsvEndpointTests
{
    private static HttpClient Authed(HttpClient c)
    {
        c.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiFactory.GenerateTestToken());
        return c;
    }

    [Fact]
    public async Task Download_WithoutAuth_Returns401()
    {
        await using var f = new ApiFactory();
        await f.InitializeAsync();

        var resp = await f.CreateClient().GetAsync($"/api/v1/imports/{Guid.NewGuid()}/errors.csv");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Download_UnknownJobId_Returns404()
    {
        await using var f = new ApiFactory();
        await f.InitializeAsync();

        var resp = await Authed(f.CreateClient()).GetAsync($"/api/v1/imports/{Guid.NewGuid()}/errors.csv");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Download_JobOwnedByAnotherUser_Returns404()
    {
        await using var f = new ApiFactory();
        await f.InitializeAsync();

        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var jobId = Guid.NewGuid();
        db.ImportJobs.Add(new ImportJob
        {
            Id = jobId, UserId = "someone-else", FileName = "x.xlsx", Status = ImportStatus.Completed,
            PreviewId = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var resp = await Authed(f.CreateClient()).GetAsync($"/api/v1/imports/{jobId}/errors.csv");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Download_JobWithErrors_ReturnsCsvWithRows()
    {
        await using var f = new ApiFactory();
        await f.InitializeAsync();

        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var jobId = Guid.NewGuid();
        db.ImportJobs.Add(new ImportJob
        {
            Id = jobId, UserId = TestAuthHandler.TestUserId, FileName = "x.xlsx", Status = ImportStatus.Completed,
            PreviewId = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow,
            ErrorCount = 1,
            Errors = """[{"Row":5,"Radicado":"123","Code":"INVALID_RADICADO","Message":"raw"}]""",
        });
        await db.SaveChangesAsync();

        var resp = await Authed(f.CreateClient()).GetAsync($"/api/v1/imports/{jobId}/errors.csv");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");

        var bytes = await resp.Content.ReadAsByteArrayAsync();
        var bom = Encoding.UTF8.GetPreamble();
        bytes.Take(bom.Length).Should().Equal(bom);
        var text = Encoding.UTF8.GetString(bytes.Skip(bom.Length).ToArray());
        text.Should().Contain("Fila,Radicado,Motivo");
        text.Should().Contain("Radicado inválido: no tiene 23 dígitos"); // code->message mapping applied
    }

    [Fact]
    public async Task Download_JobWithNoErrors_ReturnsHeaderOnlyCsv()
    {
        await using var f = new ApiFactory();
        await f.InitializeAsync();

        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var jobId = Guid.NewGuid();
        db.ImportJobs.Add(new ImportJob
        {
            Id = jobId, UserId = TestAuthHandler.TestUserId, FileName = "x.xlsx", Status = ImportStatus.Completed,
            PreviewId = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow, Errors = null,
        });
        await db.SaveChangesAsync();

        var resp = await Authed(f.CreateClient()).GetAsync($"/api/v1/imports/{jobId}/errors.csv");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var bytes = await resp.Content.ReadAsByteArrayAsync();
        var text = Encoding.UTF8.GetString(bytes.Skip(Encoding.UTF8.GetPreamble().Length).ToArray());
        text.TrimEnd('\r', '\n').Should().Be("Fila,Radicado,Motivo");
    }
}
