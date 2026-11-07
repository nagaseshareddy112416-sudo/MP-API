using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GfcEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase9AsBuiltCompliance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GfcDrawingId",
                table: "UploadedDrawings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GfcRevisionId",
                table: "UploadedDrawings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceOfCoordinates",
                table: "UploadedDrawings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "StageId",
                table: "UploadedDrawings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StageName",
                table: "UploadedDrawings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ConstructionStatus",
                table: "ExtractedFeatures",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsAsBuilt",
                table: "ExtractedFeatures",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DrawingStages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StageOrder = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsGfcBaseline = table.Column<bool>(type: "bit", nullable: false),
                    IsAsBuilt = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrawingStages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DrawingStages_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeatureMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AsBuiltDrawingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GfcDrawingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AsBuiltFeatureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GfcFeatureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MatchType = table.Column<int>(type: "int", nullable: false),
                    MatchedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureMatches_ExtractedFeatures_AsBuiltFeatureId",
                        column: x => x.AsBuiltFeatureId,
                        principalTable: "ExtractedFeatures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FeatureMatches_ExtractedFeatures_GfcFeatureId",
                        column: x => x.GfcFeatureId,
                        principalTable: "ExtractedFeatures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FeatureMatches_UploadedDrawings_AsBuiltDrawingId",
                        column: x => x.AsBuiltDrawingId,
                        principalTable: "UploadedDrawings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FeatureMatches_UploadedDrawings_GfcDrawingId",
                        column: x => x.GfcDrawingId,
                        principalTable: "UploadedDrawings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FeatureMatches_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ToleranceRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HorizontalToleranceM = table.Column<double>(type: "float", nullable: false),
                    VerticalToleranceM = table.Column<double>(type: "float", nullable: false),
                    LengthToleranceM = table.Column<double>(type: "float", nullable: false),
                    AttributeRulesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToleranceRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToleranceRules_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AsBuiltDrawingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GfcDrawingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GfcRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeatureMatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AsBuiltFeatureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GfcFeatureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ChainageStart = table.Column<double>(type: "float", nullable: true),
                    ChainageEnd = table.Column<double>(type: "float", nullable: true),
                    HorizontalDeviationM = table.Column<double>(type: "float", nullable: false),
                    AverageHorizontalDeviationM = table.Column<double>(type: "float", nullable: false),
                    VerticalDeviationM = table.Column<double>(type: "float", nullable: false),
                    LengthDeviationM = table.Column<double>(type: "float", nullable: false),
                    AttributeDiffsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplianceResults_DrawingRevisions_GfcRevisionId",
                        column: x => x.GfcRevisionId,
                        principalTable: "DrawingRevisions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ComplianceResults_ExtractedFeatures_AsBuiltFeatureId",
                        column: x => x.AsBuiltFeatureId,
                        principalTable: "ExtractedFeatures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ComplianceResults_ExtractedFeatures_GfcFeatureId",
                        column: x => x.GfcFeatureId,
                        principalTable: "ExtractedFeatures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ComplianceResults_FeatureMatches_FeatureMatchId",
                        column: x => x.FeatureMatchId,
                        principalTable: "FeatureMatches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ComplianceResults_UploadedDrawings_AsBuiltDrawingId",
                        column: x => x.AsBuiltDrawingId,
                        principalTable: "UploadedDrawings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ComplianceResults_UploadedDrawings_GfcDrawingId",
                        column: x => x.GfcDrawingId,
                        principalTable: "UploadedDrawings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ComplianceResults_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Deviations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComplianceResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Decision = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DecidedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deviations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Deviations_ComplianceResults_ComplianceResultId",
                        column: x => x.ComplianceResultId,
                        principalTable: "ComplianceResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Deviations_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_UploadedDrawings_GfcDrawingId",
                table: "UploadedDrawings",
                column: "GfcDrawingId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedDrawings_GfcRevisionId",
                table: "UploadedDrawings",
                column: "GfcRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedDrawings_StageId",
                table: "UploadedDrawings",
                column: "StageId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceResults_AsBuiltDrawingId",
                table: "ComplianceResults",
                column: "AsBuiltDrawingId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceResults_AsBuiltFeatureId",
                table: "ComplianceResults",
                column: "AsBuiltFeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceResults_FeatureMatchId",
                table: "ComplianceResults",
                column: "FeatureMatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceResults_GfcDrawingId",
                table: "ComplianceResults",
                column: "GfcDrawingId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceResults_GfcFeatureId",
                table: "ComplianceResults",
                column: "GfcFeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceResults_GfcRevisionId",
                table: "ComplianceResults",
                column: "GfcRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceResults_WorkspaceId_AsBuiltDrawingId_Status",
                table: "ComplianceResults",
                columns: new[] { "WorkspaceId", "AsBuiltDrawingId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Deviations_ComplianceResultId",
                table: "Deviations",
                column: "ComplianceResultId");

            migrationBuilder.CreateIndex(
                name: "IX_Deviations_WorkspaceId",
                table: "Deviations",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_DrawingStages_WorkspaceId_StageOrder",
                table: "DrawingStages",
                columns: new[] { "WorkspaceId", "StageOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureMatches_AsBuiltDrawingId",
                table: "FeatureMatches",
                column: "AsBuiltDrawingId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureMatches_AsBuiltFeatureId",
                table: "FeatureMatches",
                column: "AsBuiltFeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureMatches_GfcDrawingId",
                table: "FeatureMatches",
                column: "GfcDrawingId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureMatches_GfcFeatureId",
                table: "FeatureMatches",
                column: "GfcFeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureMatches_WorkspaceId",
                table: "FeatureMatches",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_ToleranceRules_WorkspaceId_Category_SubType",
                table: "ToleranceRules",
                columns: new[] { "WorkspaceId", "Category", "SubType" });

            migrationBuilder.AddForeignKey(
                name: "FK_UploadedDrawings_DrawingRevisions_GfcRevisionId",
                table: "UploadedDrawings",
                column: "GfcRevisionId",
                principalTable: "DrawingRevisions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UploadedDrawings_DrawingStages_StageId",
                table: "UploadedDrawings",
                column: "StageId",
                principalTable: "DrawingStages",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UploadedDrawings_UploadedDrawings_GfcDrawingId",
                table: "UploadedDrawings",
                column: "GfcDrawingId",
                principalTable: "UploadedDrawings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UploadedDrawings_DrawingRevisions_GfcRevisionId",
                table: "UploadedDrawings");

            migrationBuilder.DropForeignKey(
                name: "FK_UploadedDrawings_DrawingStages_StageId",
                table: "UploadedDrawings");

            migrationBuilder.DropForeignKey(
                name: "FK_UploadedDrawings_UploadedDrawings_GfcDrawingId",
                table: "UploadedDrawings");

            migrationBuilder.DropTable(
                name: "Deviations");

            migrationBuilder.DropTable(
                name: "DrawingStages");

            migrationBuilder.DropTable(
                name: "ToleranceRules");

            migrationBuilder.DropTable(
                name: "ComplianceResults");

            migrationBuilder.DropTable(
                name: "FeatureMatches");

            migrationBuilder.DropIndex(
                name: "IX_UploadedDrawings_GfcDrawingId",
                table: "UploadedDrawings");

            migrationBuilder.DropIndex(
                name: "IX_UploadedDrawings_GfcRevisionId",
                table: "UploadedDrawings");

            migrationBuilder.DropIndex(
                name: "IX_UploadedDrawings_StageId",
                table: "UploadedDrawings");

            migrationBuilder.DropColumn(
                name: "GfcDrawingId",
                table: "UploadedDrawings");

            migrationBuilder.DropColumn(
                name: "GfcRevisionId",
                table: "UploadedDrawings");

            migrationBuilder.DropColumn(
                name: "SourceOfCoordinates",
                table: "UploadedDrawings");

            migrationBuilder.DropColumn(
                name: "StageId",
                table: "UploadedDrawings");

            migrationBuilder.DropColumn(
                name: "StageName",
                table: "UploadedDrawings");

            migrationBuilder.DropColumn(
                name: "ConstructionStatus",
                table: "ExtractedFeatures");

            migrationBuilder.DropColumn(
                name: "IsAsBuilt",
                table: "ExtractedFeatures");
        }
    }
}
