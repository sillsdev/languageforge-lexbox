using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LcmCrdt.Migrations
{
    /// <inheritdoc />
    public partial class AddFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.CreateTable(
            //     name: "EntrySearchRecord",
            //     columns: table => new
            //     {
            //         RowId = table.Column<int>(type: "INTEGER", nullable: false)
            //             .Annotation("Sqlite:Autoincrement", true),
            //         Id = table.Column<Guid>(type: "TEXT", nullable: false),
            //         Headword = table.Column<string>(type: "TEXT", nullable: false),
            //         EntrySearchRecord = table.Column<string>(type: "TEXT", nullable: false),
            //         Rank = table.Column<double>(type: "REAL", nullable: true)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_EntrySearchRecord", x => x.RowId);
            //     });
            migrationBuilder.Sql(@"
                CREATE VIRTUAL TABLE EntrySearchRecord USING fts5(Headword, Definition, Gloss, Id UNINDEXED, tokenize=""trigram"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntrySearchRecord");
        }
    }
}
