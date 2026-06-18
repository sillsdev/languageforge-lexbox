using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LexData.Migrations
{
    /// <inheritdoc />
    public partial class ChangeCrdtCommitsPkToCompositeProjectIdId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CrdtCommits",
                table: "CrdtCommits");

            migrationBuilder.DropIndex(
                name: "IX_CrdtCommits_ProjectId",
                table: "CrdtCommits");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CrdtCommits",
                table: "CrdtCommits",
                columns: new[] { "ProjectId", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CrdtCommits",
                table: "CrdtCommits");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CrdtCommits",
                table: "CrdtCommits",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_CrdtCommits_ProjectId",
                table: "CrdtCommits",
                column: "ProjectId");
        }
    }
}
