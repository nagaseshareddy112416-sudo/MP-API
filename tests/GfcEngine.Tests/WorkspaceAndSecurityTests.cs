using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using GfcEngine.Domain.Entities;
using GfcEngine.Domain.Enums;
using GfcEngine.Infrastructure.Persistence;
using Xunit;

namespace GfcEngine.Tests;

public class WorkspaceAndSecurityTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task WorkspaceIsolation_DrawingsInWorkspaceA_AreNotVisibleInWorkspaceB()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var wsA = new Workspace { Id = Guid.NewGuid(), Name = "Workspace A", Code = "WS-A" };
        var wsB = new Workspace { Id = Guid.NewGuid(), Name = "Workspace B", Code = "WS-B" };

        context.Workspaces.AddRange(wsA, wsB);

        var drawingA = new UploadedDrawing
        {
            Id = Guid.NewGuid(),
            WorkspaceId = wsA.Id,
            ProjectName = "Project Alpha",
            Category = "Road",
            SubType = "Centerline"
        };

        var drawingB = new UploadedDrawing
        {
            Id = Guid.NewGuid(),
            WorkspaceId = wsB.Id,
            ProjectName = "Project Beta",
            Category = "Utility",
            SubType = "Water"
        };

        context.UploadedDrawings.AddRange(drawingA, drawingB);
        await context.SaveChangesAsync();

        // Act - Query scoped to Workspace A
        var drawingsWsA = await context.UploadedDrawings
            .Where(d => d.WorkspaceId == wsA.Id)
            .ToListAsync();

        // Assert
        drawingsWsA.Should().HaveCount(1);
        drawingsWsA.First().ProjectName.Should().Be("Project Alpha");
        drawingsWsA.Any(d => d.WorkspaceId == wsB.Id).Should().BeFalse();
    }

    [Fact]
    public async Task WorkspaceRoles_AssignRolesCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var ws = new Workspace { Id = Guid.NewGuid(), Name = "Test WS", Code = "WS-TEST" };
        context.Workspaces.Add(ws);

        var viewerRole = new WorkspaceUserRole
        {
            Id = Guid.NewGuid(),
            WorkspaceId = ws.Id,
            UserId = "user-viewer-123",
            Role = Roles.Viewer
        };

        var adminRole = new WorkspaceUserRole
        {
            Id = Guid.NewGuid(),
            WorkspaceId = ws.Id,
            UserId = "user-admin-456",
            Role = Roles.WorkspaceAdmin
        };

        context.WorkspaceUserRoles.AddRange(viewerRole, adminRole);
        await context.SaveChangesAsync();

        // Act & Assert
        var userRole = await context.WorkspaceUserRoles
            .FirstOrDefaultAsync(r => r.WorkspaceId == ws.Id && r.UserId == "user-viewer-123");

        userRole.Should().NotBeNull();
        userRole!.Role.Should().Be(Roles.Viewer);
        (userRole.Role == Roles.WorkspaceAdmin).Should().BeFalse();
    }
}

