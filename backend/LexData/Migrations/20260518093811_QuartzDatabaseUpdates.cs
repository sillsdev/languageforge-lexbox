using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LexData.Migrations
{
    /// <inheritdoc />
    public partial class QuartzDatabaseUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "misfire_orig_fire_time",
                schema: "quartz",
                table: "qrtz_triggers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "OpenIddictTokens",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "misfire_orig_fire_time",
                schema: "quartz",
                table: "qrtz_triggers");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "OpenIddictTokens",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldNullable: true);
        }
    }
}
