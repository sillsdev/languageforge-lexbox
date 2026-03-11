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
            // Using raw SQL here because we need ON CONFLICT IGNORE on the unique constraint
            // for MorphType, and EF Core doesn't have an API that would add that clause
            migrationBuilder.Sql(@"
                CREATE TABLE MorphTypeData (
                    Id             TEXT    NOT NULL,
                    MorphType      INTEGER NOT NULL UNIQUE ON CONFLICT IGNORE,
                    Name           TEXT    NOT NULL,
                    Abbreviation   TEXT    NOT NULL,
                    Description    TEXT    NOT NULL,
                    LeadingToken   TEXT,
                    TrailingToken  TEXT,
                    SecondaryOrder INTEGER NOT NULL,
                    DeletedAt      TEXT,
                    SnapshotId     TEXT,
                    CONSTRAINT PK_MorphTypeData PRIMARY KEY (Id),
                    CONSTRAINT FK_MorphTypeData_Snapshots_SnapshotId
                        FOREIGN KEY (SnapshotId) REFERENCES Snapshots (Id) ON DELETE SET NULL
                );
            ");

            migrationBuilder.CreateIndex(
                name: "IX_MorphTypeData_MorphType",
                table: "MorphTypeData",
                column: "MorphType",
                unique: true);
            // Note that unique: true above is not strictly necessary, but keeps consistent with the line
            // morphTypeDataModel.HasIndex(m => m.MorphType).IsUnique() in LcmCrdtDbContext.OnModelCreating()
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
