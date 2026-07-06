using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LcmCrdt.Migrations
{
    /// <inheritdoc />
    public partial class AddWritingSystemIsDisabled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDisabled",
                table: "WritingSystem",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDisabled",
                table: "WritingSystem");
        }
    }
}
