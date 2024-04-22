using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LexData.Migrations
{
    /// <inheritdoc />
    public partial class MakeDraftFilterProjectMangerIdaFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DraftProjects_ProjectManagerId",
                table: "DraftProjects",
                column: "ProjectManagerId");

            migrationBuilder.AddForeignKey(
                name: "FK_DraftProjects_Users_ProjectManagerId",
                table: "DraftProjects",
                column: "ProjectManagerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DraftProjects_Users_ProjectManagerId",
                table: "DraftProjects");

            migrationBuilder.DropIndex(
                name: "IX_DraftProjects_ProjectManagerId",
                table: "DraftProjects");
        }
    }
}
