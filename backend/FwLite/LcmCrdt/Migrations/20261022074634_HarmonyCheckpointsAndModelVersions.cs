using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LcmCrdt.Migrations
{
    /// <inheritdoc />
    public partial class HarmonyCheckpointsAndModelVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSnapshotCheckpoint",
                table: "Commits",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AuthoredModelVersion",
                table: "ChangeEntities",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "LocalState",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalState", x => x.Key);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Commits_Checkpoint",
                table: "Commits",
                columns: new[] { "DateTime", "Counter", "Id" },
                filter: "\"IsSnapshotCheckpoint\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Commits_Checkpoint",
                table: "Commits");

            migrationBuilder.DropTable(
                name: "LocalState");

            migrationBuilder.DropColumn(
                name: "IsSnapshotCheckpoint",
                table: "Commits");

            migrationBuilder.DropColumn(
                name: "AuthoredModelVersion",
                table: "ChangeEntities");
        }
    }
}
