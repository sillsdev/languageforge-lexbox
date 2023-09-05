using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LexData.Migrations
{
    /// <inheritdoc />
    public partial class addprojectparent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                table: "Projects",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ParentId",
                table: "Projects",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Projects_ParentId",
                table: "Projects",
                column: "ParentId",
                principalTable: "Projects",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Projects_ParentId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_ParentId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "Projects");
        }
    }
}
