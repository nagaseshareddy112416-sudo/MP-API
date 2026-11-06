using System.Globalization;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using GfcEngine.Domain.Entities;
using GfcEngine.Domain.Enums;
using GfcEngine.Infrastructure.Persistence;

namespace GfcEngine.Infrastructure.Services;

public class SeedDataService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SeedDataService> _logger;

    public SeedDataService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        ILogger<SeedDataService> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("Checking database seed data...");

        // 1. Roles
        string[] roles = { Roles.WorkspaceAdmin, Roles.Uploader, Roles.Reviewer, Roles.Viewer };
        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Default Workspace
        var defaultWorkspace = await _context.Workspaces.FirstOrDefaultAsync(w => w.Code == "TEST-WS-01");
        if (defaultWorkspace == null)
        {
            // Look for workspace_config.json
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gis_test_pack", "docs", "workspace_config.json");
            if (!File.Exists(configPath))
            {
                configPath = Path.Combine(Directory.GetCurrentDirectory(), "gis_test_pack", "docs", "workspace_config.json");
            }
            if (!File.Exists(configPath))
            {
                configPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "gis_test_pack", "docs", "workspace_config.json");
            }

            string wsName = "Test Workspace (synthetic)";
            int defaultEpsg = 32644;
            int displayEpsg = 4326;
            double centerLon = 80.516;
            double centerLat = 16.513;
            int defaultZoom = 14;
            double tolerance = 0.5;
            double farDistance = 5000.0;

            if (File.Exists(configPath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(configPath);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("name", out var pName)) wsName = pName.GetString() ?? wsName;
                    if (root.TryGetProperty("default_epsg", out var pEpsg)) defaultEpsg = pEpsg.GetInt32();
                    if (root.TryGetProperty("display_epsg", out var pDisp)) displayEpsg = pDisp.GetInt32();
                    if (root.TryGetProperty("default_zoom", out var pZoom)) defaultZoom = pZoom.GetInt32();
                    if (root.TryGetProperty("clash_vertical_tolerance_m", out var pTol)) tolerance = pTol.GetDouble();
                    if (root.TryGetProperty("far_from_existing_data_warning_m", out var pFar)) farDistance = pFar.GetDouble();
                    if (root.TryGetProperty("centre_lonlat", out var pCoords) && pCoords.GetArrayLength() == 2)
                    {
                        centerLon = pCoords[0].GetDouble();
                        centerLat = pCoords[1].GetDouble();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not read workspace_config.json, using defaults.");
                }
            }

            defaultWorkspace = new Workspace
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = wsName,
                Code = "TEST-WS-01",
                DefaultEpsg = defaultEpsg,
                DisplayEpsg = displayEpsg,
                CenterLon = centerLon,
                CenterLat = centerLat,
                DefaultZoom = defaultZoom,
                ClashVerticalToleranceM = tolerance,
                FarFromExistingDataWarningM = farDistance,
                CreatedAt = DateTime.UtcNow
            };

            _context.Workspaces.Add(defaultWorkspace);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created default workspace '{Name}' with code {Code}", defaultWorkspace.Name, defaultWorkspace.Code);
        }

        // 3. Layer Mappings from layer_mapping.csv
        var existingMappingsCount = await _context.CadLayerMappings.CountAsync(m => m.WorkspaceId == defaultWorkspace.Id);
        if (existingMappingsCount == 0)
        {
            var csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gis_test_pack", "docs", "layer_mapping.csv");
            if (!File.Exists(csvPath))
            {
                csvPath = Path.Combine(Directory.GetCurrentDirectory(), "gis_test_pack", "docs", "layer_mapping.csv");
            }
            if (!File.Exists(csvPath))
            {
                csvPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "gis_test_pack", "docs", "layer_mapping.csv");
            }

            var mappings = new List<CadLayerMapping>();
            if (File.Exists(csvPath))
            {
                using var reader = new StreamReader(csvPath);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true });
                var records = csv.GetRecords<dynamic>();
                foreach (var rec in records)
                {
                    var d = (IDictionary<string, object>)rec;
                    mappings.Add(new CadLayerMapping
                    {
                        WorkspaceId = defaultWorkspace.Id,
                        CadLayer = d["cad_layer"].ToString() ?? "",
                        Category = d["category"].ToString() ?? "",
                        SubType = d["sub_type"].ToString() ?? "",
                        GeometryType = d["geometry"].ToString() ?? "line",
                        Color = d["color"].ToString() ?? "#000000"
                    });
                }
            }
            else
            {
                // Fallback default mappings
                mappings.AddRange(new[]
                {
                    new CadLayerMapping { WorkspaceId = defaultWorkspace.Id, CadLayer = "ROAD_CL", Category = "Road", SubType = "Centerline", GeometryType = "line", Color = "#000000" },
                    new CadLayerMapping { WorkspaceId = defaultWorkspace.Id, CadLayer = "ROAD_EDGE", Category = "Road", SubType = "Edge of pavement", GeometryType = "line", Color = "#555555" },
                    new CadLayerMapping { WorkspaceId = defaultWorkspace.Id, CadLayer = "CHAINAGE", Category = "Road", SubType = "Chainage marker", GeometryType = "point", Color = "#ff9800" },
                    new CadLayerMapping { WorkspaceId = defaultWorkspace.Id, CadLayer = "WATER_MAIN", Category = "Utility", SubType = "Water", GeometryType = "line", Color = "#1f77ff" },
                    new CadLayerMapping { WorkspaceId = defaultWorkspace.Id, CadLayer = "SEWER_LINE", Category = "Utility", SubType = "Sewer", GeometryType = "line", Color = "#8b4513" },
                    new CadLayerMapping { WorkspaceId = defaultWorkspace.Id, CadLayer = "STORM_DRAIN", Category = "Utility", SubType = "Storm drain", GeometryType = "line", Color = "#2ca02c" },
                    new CadLayerMapping { WorkspaceId = defaultWorkspace.Id, CadLayer = "POWER_CABLE", Category = "Utility", SubType = "Power", GeometryType = "line", Color = "#e63946" },
                    new CadLayerMapping { WorkspaceId = defaultWorkspace.Id, CadLayer = "BLDG_FOOTPRINT", Category = "Building", SubType = "Footprint", GeometryType = "polygon", Color = "#6c757d" }
                });
            }

            _context.CadLayerMappings.AddRange(mappings);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} CAD layer mappings for workspace", mappings.Count);
        }

        // 4. Category Configs
        var catCount = await _context.CategoryConfigs.CountAsync(c => c.WorkspaceId == defaultWorkspace.Id);
        if (catCount == 0)
        {
            var categories = new List<CategoryConfig>
            {
                new() { WorkspaceId = defaultWorkspace.Id, CategoryName = "Road", SubTypeName = "Centerline", ColorHex = "#000000", IconName = "alt_route", DisplayOrder = 1 },
                new() { WorkspaceId = defaultWorkspace.Id, CategoryName = "Road", SubTypeName = "Edge of pavement", ColorHex = "#555555", IconName = "linear_scale", DisplayOrder = 2 },
                new() { WorkspaceId = defaultWorkspace.Id, CategoryName = "Utility", SubTypeName = "Water", ColorHex = "#1f77ff", IconName = "water_drop", DisplayOrder = 3 },
                new() { WorkspaceId = defaultWorkspace.Id, CategoryName = "Utility", SubTypeName = "Sewer", ColorHex = "#8b4513", IconName = "plumbing", DisplayOrder = 4 },
                new() { WorkspaceId = defaultWorkspace.Id, CategoryName = "Utility", SubTypeName = "Storm drain", ColorHex = "#2ca02c", IconName = "waves", DisplayOrder = 5 },
                new() { WorkspaceId = defaultWorkspace.Id, CategoryName = "Utility", SubTypeName = "Power", ColorHex = "#e63946", IconName = "bolt", DisplayOrder = 6 },
                new() { WorkspaceId = defaultWorkspace.Id, CategoryName = "Building", SubTypeName = "Footprint", ColorHex = "#6c757d", IconName = "apartment", DisplayOrder = 7 }
            };

            _context.CategoryConfigs.AddRange(categories);
            await _context.SaveChangesAsync();
        }

        // 5. Dynamic Category Form Field Configs
        var fieldCount = await _context.CategoryFieldConfigs.CountAsync(f => f.WorkspaceId == defaultWorkspace.Id);
        if (fieldCount == 0)
        {
            var fields = new List<CategoryFieldConfig>
            {
                // Road fields
                new() { WorkspaceId = defaultWorkspace.Id, CategoryName = "Road", FieldName = "ChainageStart", FieldLabel = "Chainage Start (m)", FieldType = "number", IsRequired = false, DisplayOrder = 1 },
                new() { WorkspaceId = defaultWorkspace.Id, CategoryName = "Road", FieldName = "ChainageEnd", FieldLabel = "Chainage End (m)", FieldType = "number", IsRequired = false, DisplayOrder = 2 },
                new() { WorkspaceId = defaultWorkspace.Id, CategoryName = "Road", FieldName = "Contractor", FieldLabel = "Contractor", FieldType = "text", IsRequired = false, DisplayOrder = 3 },
                new() { WorkspaceId = defaultWorkspace.Id, CategoryName = "Road", FieldName = "Consultant", FieldLabel = "Consultant", FieldType = "text", IsRequired = false, DisplayOrder = 4 },

                // Utility fields
                new() { WorkspaceId = defaultWorkspace.Id, CategoryName = "Utility", FieldName = "Material", FieldLabel = "Pipe/Cable Material", FieldType = "text", IsRequired = false, DisplayOrder = 1 },
                new() { WorkspaceId = defaultWorkspace.Id, CategoryName = "Utility", FieldName = "Contractor", FieldLabel = "Contractor", FieldType = "text", IsRequired = false, DisplayOrder = 2 },
                new() { WorkspaceId = defaultWorkspace.Id, CategoryName = "Utility", FieldName = "Consultant", FieldLabel = "Consultant", FieldType = "text", IsRequired = false, DisplayOrder = 3 },

                // Building fields
                new() { WorkspaceId = defaultWorkspace.Id, CategoryName = "Building", FieldName = "PlotNumber", FieldLabel = "Plot Number", FieldType = "text", IsRequired = false, DisplayOrder = 1 },
                new() { WorkspaceId = defaultWorkspace.Id, CategoryName = "Building", FieldName = "Floors", FieldLabel = "Floors / Height", FieldType = "text", IsRequired = false, DisplayOrder = 2 }
            };

            _context.CategoryFieldConfigs.AddRange(fields);
            await _context.SaveChangesAsync();
        }

        // 6. Admin User
        var adminEmail = _configuration["AdminUser:Email"] ?? "admin@gfcengine.com";
        var adminPassword = _configuration["AdminUser:DefaultPassword"] ?? "Admin@Gfc2026!";

        var adminUser = await _userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                EmailConfirmed = true,
                MustChangePassword = true,
                CreatedAt = DateTime.UtcNow
            };

            var createRes = await _userManager.CreateAsync(adminUser, adminPassword);
            if (createRes.Succeeded)
            {
                await _userManager.AddToRoleAsync(adminUser, Roles.WorkspaceAdmin);
                _logger.LogInformation("======================================================");
                _logger.LogInformation("SEED ADMIN USER CREATED:");
                _logger.LogInformation("Username: {Email}", adminEmail);
                _logger.LogInformation("Password: [Configured in appsettings/env]");
                _logger.LogInformation("MustChangePassword: true");
                _logger.LogInformation("======================================================");

                // Assign to default workspace
                _context.WorkspaceUserRoles.Add(new WorkspaceUserRole
                {
                    WorkspaceId = defaultWorkspace.Id,
                    UserId = adminUser.Id,
                    Role = Roles.WorkspaceAdmin
                });
                await _context.SaveChangesAsync();
            }
            else
            {
                _logger.LogError("Failed to create admin user: {Errors}", string.Join(", ", createRes.Errors.Select(e => e.Description)));
            }
        }
    }
}
