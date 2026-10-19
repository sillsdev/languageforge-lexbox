using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LcmCrdt.Migrations
{
    /// <inheritdoc />
    public partial class AddSemanticDomainExtendedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Abbreviation",
                table: "SemanticDomain",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "SemanticDomain",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<string>(
                name: "LouwNidaCodes",
                table: "SemanticDomain",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OcmCodes",
                table: "SemanticDomain",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Abbreviation",
                table: "SemanticDomain");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "SemanticDomain");

            migrationBuilder.DropColumn(
                name: "LouwNidaCodes",
                table: "SemanticDomain");

            migrationBuilder.DropColumn(
                name: "OcmCodes",
                table: "SemanticDomain");
        }
    }
}
