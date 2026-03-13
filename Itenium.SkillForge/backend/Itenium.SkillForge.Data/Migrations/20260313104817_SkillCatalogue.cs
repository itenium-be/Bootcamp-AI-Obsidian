using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Itenium.SkillForge.Data.Migrations
{
    /// <inheritdoc />
    public partial class SkillCatalogue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConsultantProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultantProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsultantProfiles_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Skills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LevelCount = table.Column<int>(type: "integer", nullable: false),
                    IsUniversal = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConsultantSkillProgress",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SkillId = table.Column<int>(type: "integer", nullable: false),
                    AchievedLevel = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultantSkillProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsultantSkillProgress_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeniorityThresholds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    SeniorityLevel = table.Column<int>(type: "integer", nullable: false),
                    SkillId = table.Column<int>(type: "integer", nullable: false),
                    MinimumLevel = table.Column<int>(type: "integer", nullable: false)
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
                    table.ForeignKey(
                        name: "FK_SeniorityThresholds_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkillLevelDescriptors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SkillId = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillLevelDescriptors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillLevelDescriptors_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkillPrerequisites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SkillId = table.Column<int>(type: "integer", nullable: false),
                    PrerequisiteSkillId = table.Column<int>(type: "integer", nullable: false),
                    RequiredLevel = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillPrerequisites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillPrerequisites_Skills_PrerequisiteSkillId",
                        column: x => x.PrerequisiteSkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SkillPrerequisites_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsultantProfiles_TeamId",
                table: "ConsultantProfiles",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultantProfiles_UserId",
                table: "ConsultantProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsultantSkillProgress_SkillId",
                table: "ConsultantSkillProgress",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_SeniorityThresholds_SkillId",
                table: "SeniorityThresholds",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_SeniorityThresholds_TeamId",
                table: "SeniorityThresholds",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillLevelDescriptors_SkillId",
                table: "SkillLevelDescriptors",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillPrerequisites_PrerequisiteSkillId",
                table: "SkillPrerequisites",
                column: "PrerequisiteSkillId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillPrerequisites_SkillId",
                table: "SkillPrerequisites",
                column: "SkillId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsultantProfiles");

            migrationBuilder.DropTable(
                name: "ConsultantSkillProgress");

            migrationBuilder.DropTable(
                name: "SeniorityThresholds");

            migrationBuilder.DropTable(
                name: "SkillLevelDescriptors");

            migrationBuilder.DropTable(
                name: "SkillPrerequisites");

            migrationBuilder.DropTable(
                name: "Skills");
        }
    }
}
