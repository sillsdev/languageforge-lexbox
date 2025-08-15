using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LcmCrdt.Migrations
{
    /// <inheritdoc />
    public partial class AddCodeToProjectData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "ProjectData",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql("UPDATE ProjectData SET Code = Name");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "ProjectData",
                type: "TEXT",
                nullable: false,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Code",
                table: "ProjectData");
        }
    }
}
