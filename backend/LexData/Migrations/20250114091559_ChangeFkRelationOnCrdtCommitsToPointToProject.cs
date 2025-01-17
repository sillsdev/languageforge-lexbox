using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LexData.Migrations
{
    /// <inheritdoc />
    public partial class ChangeFkRelationOnCrdtCommitsToPointToProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CrdtCommits_FlexProjectMetadata_ProjectId",
                table: "CrdtCommits");

            migrationBuilder.AddForeignKey(
                name: "FK_CrdtCommits_Projects_ProjectId",
                table: "CrdtCommits",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CrdtCommits_Projects_ProjectId",
                table: "CrdtCommits");

            migrationBuilder.AddForeignKey(
                name: "FK_CrdtCommits_FlexProjectMetadata_ProjectId",
                table: "CrdtCommits",
                column: "ProjectId",
                principalTable: "FlexProjectMetadata",
                principalColumn: "ProjectId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
