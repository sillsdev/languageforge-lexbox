using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LexData.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectLangTagsToMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WritingSystems",
                table: "FlexProjectMetadata",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WritingSystems",
                table: "FlexProjectMetadata");
        }
    }
}
