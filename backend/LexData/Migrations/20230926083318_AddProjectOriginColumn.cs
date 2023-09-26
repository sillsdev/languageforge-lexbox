using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LexData.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectOriginColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectOrigin",
                table: "Projects",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProjectOrigin",
                table: "Projects");
        }
    }
}
