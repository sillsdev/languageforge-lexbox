using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LcmCrdt.Migrations
{
    /// <inheritdoc />
    public partial class RegenerateSearchTableForMorphTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Force FTS rebuild so headwords include morph-type prefix/postfix tokens
            migrationBuilder.Sql("DELETE FROM EntrySearchRecord;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // FTS table will be lazily regenerated
        }
    }
}
