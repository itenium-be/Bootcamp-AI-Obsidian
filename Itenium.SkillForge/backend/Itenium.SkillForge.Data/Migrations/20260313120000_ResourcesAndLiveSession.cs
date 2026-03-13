using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Itenium.SkillForge.Data.Migrations
{
    /// <inheritdoc />
    public partial class ResourcesAndLiveSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Skills (already in AppDbContext but not yet migrated)
            migrationBuilder.CreateTable(
                name: "Skills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LevelCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    LevelDescriptorsJson = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    PrerequisitesJson = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skills", x => x.Id);
                });

            // SkillProfiles
            migrationBuilder.CreateTable(
                name: "SkillProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SkillId = table.Column<int>(type: "integer", nullable: false),
                    Profile = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillProfiles_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SkillProfiles_SkillId_Profile",
                table: "SkillProfiles",
                columns: new[] { "SkillId", "Profile" },
                unique: true);

            // ConsultantProfiles
            migrationBuilder.CreateTable(
                name: "ConsultantProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Profile = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedBy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultantProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsultantProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsultantProfiles_UserId",
                table: "ConsultantProfiles",
                column: "UserId",
                unique: true);

            // SeniorityThresholds
            migrationBuilder.CreateTable(
                name: "SeniorityThresholds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Profile = table.Column<int>(type: "integer", nullable: false),
                    SeniorityLevel = table.Column<int>(type: "integer", nullable: false),
                    SkillId = table.Column<int>(type: "integer", nullable: false),
                    MinNiveau = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeniorityThresholds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeniorityThresholds_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SeniorityThresholds_Profile_SeniorityLevel_SkillId",
                table: "SeniorityThresholds",
                columns: new[] { "Profile", "SeniorityLevel", "SkillId" },
                unique: true);

            // Goals
            migrationBuilder.CreateTable(
                name: "Goals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsultantId = table.Column<string>(type: "text", nullable: false),
                    CoachId = table.Column<string>(type: "text", nullable: false),
                    SkillId = table.Column<int>(type: "integer", nullable: false),
                    CurrentNiveau = table.Column<int>(type: "integer", nullable: false),
                    TargetNiveau = table.Column<int>(type: "integer", nullable: false),
                    Deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LinkedResourceIds = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Goals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Goals_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Goals_ConsultantId",
                table: "Goals",
                column: "ConsultantId");

            // ReadinessFlags
            migrationBuilder.CreateTable(
                name: "ReadinessFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoalId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsultantId = table.Column<string>(type: "text", nullable: false),
                    RaisedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadinessFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReadinessFlags_Goals_GoalId",
                        column: x => x.GoalId,
                        principalTable: "Goals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReadinessFlags_GoalId_IsActive",
                table: "ReadinessFlags",
                columns: new[] { "GoalId", "IsActive" });

            // Resources (Story #25)
            migrationBuilder.CreateTable(
                name: "Resources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    SkillId = table.Column<int>(type: "integer", nullable: false),
                    FromNiveau = table.Column<int>(type: "integer", nullable: false),
                    ToNiveau = table.Column<int>(type: "integer", nullable: false),
                    ContributedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ContributedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ThumbsUp = table.Column<int>(type: "integer", nullable: false),
                    ThumbsDown = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Resources_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_SkillId",
                table: "Resources",
                column: "SkillId");

            // ResourceCompletions (Story #27)
            migrationBuilder.CreateTable(
                name: "ResourceCompletions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsultantId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    GoalId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceCompletions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceCompletions_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResourceCompletions_Goals_GoalId",
                        column: x => x.GoalId,
                        principalTable: "Goals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceCompletions_ResourceId",
                table: "ResourceCompletions",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceCompletions_ConsultantId",
                table: "ResourceCompletions",
                column: "ConsultantId");

            // ResourceRatings (Story #28)
            migrationBuilder.CreateTable(
                name: "ResourceRatings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    RatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceRatings_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRatings_ResourceId_UserId",
                table: "ResourceRatings",
                columns: new[] { "ResourceId", "UserId" },
                unique: true);

            // CoachingSessions (Story #31)
            migrationBuilder.CreateTable(
                name: "CoachingSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CoachId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ConsultantId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachingSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoachingSessions_ConsultantId",
                table: "CoachingSessions",
                column: "ConsultantId");

            // Validations (Story #32)
            migrationBuilder.CreateTable(
                name: "Validations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillId = table.Column<int>(type: "integer", nullable: false),
                    ConsultantId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ValidatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ValidatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FromNiveau = table.Column<int>(type: "integer", nullable: false),
                    ToNiveau = table.Column<int>(type: "integer", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Validations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Validations_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Validations_CoachingSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "CoachingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Validations_ConsultantId",
                table: "Validations",
                column: "ConsultantId");

            migrationBuilder.CreateIndex(
                name: "IX_Validations_SessionId",
                table: "Validations",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Validations");
            migrationBuilder.DropTable(name: "CoachingSessions");
            migrationBuilder.DropTable(name: "ResourceRatings");
            migrationBuilder.DropTable(name: "ResourceCompletions");
            migrationBuilder.DropTable(name: "Resources");
            migrationBuilder.DropTable(name: "ReadinessFlags");
            migrationBuilder.DropTable(name: "Goals");
            migrationBuilder.DropTable(name: "SeniorityThresholds");
            migrationBuilder.DropTable(name: "ConsultantProfiles");
            migrationBuilder.DropTable(name: "SkillProfiles");
            migrationBuilder.DropTable(name: "Skills");
        }
    }
}
