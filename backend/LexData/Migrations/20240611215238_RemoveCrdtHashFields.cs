using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LexData.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCrdtHashFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Hash",
                table: "CrdtCommits");

            migrationBuilder.DropColumn(
                name: "ParentHash",
                table: "CrdtCommits");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Hash",
                table: "CrdtCommits",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentHash",
                table: "CrdtCommits",
                type: "text",
                nullable: true);
        }
    }
}
