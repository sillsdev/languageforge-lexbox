using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LcmCrdt.Migrations
{
    /// <inheritdoc />
    public partial class SetPosIdNullOnDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sense_PartOfSpeech_PartOfSpeechId",
                table: "Sense");

            migrationBuilder.AddForeignKey(
                name: "FK_Sense_PartOfSpeech_PartOfSpeechId",
                table: "Sense",
                column: "PartOfSpeechId",
                principalTable: "PartOfSpeech",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sense_PartOfSpeech_PartOfSpeechId",
                table: "Sense");

            migrationBuilder.AddForeignKey(
                name: "FK_Sense_PartOfSpeech_PartOfSpeechId",
                table: "Sense",
                column: "PartOfSpeechId",
                principalTable: "PartOfSpeech",
                principalColumn: "Id");
        }
    }
}
