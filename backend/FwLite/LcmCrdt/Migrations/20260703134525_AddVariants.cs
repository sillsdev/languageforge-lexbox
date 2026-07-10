using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LcmCrdt.Migrations
{
    /// <inheritdoc />
    public partial class AddVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Variants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    VariantEntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VariantHeadword = table.Column<string>(type: "TEXT", nullable: true),
                    MainEntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MainSenseId = table.Column<Guid>(type: "TEXT", nullable: true),
                    MainHeadword = table.Column<string>(type: "TEXT", nullable: true),
                    Types = table.Column<string>(type: "jsonb", nullable: false),
                    HideMinorEntry = table.Column<bool>(type: "INTEGER", nullable: false),
                    Comment = table.Column<string>(type: "jsonb", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Variants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Variants_Entry_MainEntryId",
                        column: x => x.MainEntryId,
                        principalTable: "Entry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Variants_Entry_VariantEntryId",
                        column: x => x.VariantEntryId,
                        principalTable: "Entry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Variants_Sense_MainSenseId",
                        column: x => x.MainSenseId,
                        principalTable: "Sense",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Variants_Snapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "Snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "VariantType",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "jsonb", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariantType", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VariantType_Snapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "Snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Variants_MainEntryId",
                table: "Variants",
                column: "MainEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_Variants_MainSenseId",
                table: "Variants",
                column: "MainSenseId");

            migrationBuilder.CreateIndex(
                name: "IX_Variants_SnapshotId",
                table: "Variants",
                column: "SnapshotId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Variants_VariantEntryId_MainEntryId",
                table: "Variants",
                columns: new[] { "VariantEntryId", "MainEntryId" },
                unique: true,
                filter: "MainSenseId IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Variants_VariantEntryId_MainEntryId_MainSenseId",
                table: "Variants",
                columns: new[] { "VariantEntryId", "MainEntryId", "MainSenseId" },
                unique: true,
                filter: "MainSenseId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VariantType_SnapshotId",
                table: "VariantType",
                column: "SnapshotId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Variants");

            migrationBuilder.DropTable(
                name: "VariantType");
        }
    }
}
