using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LcmCrdt.Migrations
{
    /// <inheritdoc />
    public partial class AddEntrySearchTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE VIRTUAL TABLE EntrySearchRecord USING fts5(Headword, LexemeForm, CitationForm, Definition, Gloss, Id UNINDEXED, tokenize=""trigram remove_diacritics 1"");
                INSERT INTO EntrySearchRecord(EntrySearchRecord, rank) VALUES('rank', 'bm25(5, 3, 4, 1, 2)');
            ");
            //'bm25(5, 3, 4, 1, 2)' configures the weights for each column and how they are matched
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntrySearchRecord");
        }
    }
}
