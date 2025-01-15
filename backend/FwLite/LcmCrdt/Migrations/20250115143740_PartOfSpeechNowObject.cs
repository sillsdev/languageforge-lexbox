using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LcmCrdt.Migrations
{
    /// <inheritdoc />
    public partial class PartOfSpeechNowObject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PartOfSpeech",
                table: "Sense");

            migrationBuilder.CreateIndex(
                name: "IX_Sense_PartOfSpeechId",
                table: "Sense",
                column: "PartOfSpeechId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sense_PartOfSpeech_PartOfSpeechId",
                table: "Sense",
                column: "PartOfSpeechId",
                principalTable: "PartOfSpeech",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sense_PartOfSpeech_PartOfSpeechId",
                table: "Sense");

            migrationBuilder.DropIndex(
                name: "IX_Sense_PartOfSpeechId",
                table: "Sense");

            migrationBuilder.AddColumn<string>(
                name: "PartOfSpeech",
                table: "Sense",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
