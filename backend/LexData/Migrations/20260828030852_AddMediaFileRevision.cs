using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LexData.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaFileRevision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing rows are all backed (uploaded) files, so backfill them to revision 1. Revision 0 is a
            // reserved sentinel for a pending reservation with no binary yet, which only new code creates.
            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "Files",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Revision",
                table: "Files");
        }
    }
}
