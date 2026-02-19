using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LcmCrdt.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomView",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Base = table.Column<int>(type: "INTEGER", nullable: false),
                    EntryFields = table.Column<string>(type: "jsonb", nullable: false),
                    SenseFields = table.Column<string>(type: "jsonb", nullable: false),
                    ExampleFields = table.Column<string>(type: "jsonb", nullable: false),
                    Vernacular = table.Column<string>(type: "jsonb", nullable: true),
                    Analysis = table.Column<string>(type: "jsonb", nullable: true),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomView", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomView_Snapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "Snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomView_SnapshotId",
                table: "CustomView",
                column: "SnapshotId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomView");
        }
    }
}
