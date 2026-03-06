using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LcmCrdt.Migrations
{
    /// <inheritdoc />
    public partial class AddMorphTypeDataTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MorphTypeData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MorphType = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "jsonb", nullable: false),
                    Abbreviation = table.Column<string>(type: "jsonb", nullable: false),
                    Description = table.Column<string>(type: "jsonb", nullable: false),
                    LeadingToken = table.Column<string>(type: "TEXT", nullable: true),
                    TrailingToken = table.Column<string>(type: "TEXT", nullable: true),
                    SecondaryOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MorphTypeData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MorphTypeData_Snapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "Snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MorphTypeData_SnapshotId",
                table: "MorphTypeData",
                column: "SnapshotId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MorphTypeData");
        }
    }
}
