using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LexData.Migrations
{
    /// <inheritdoc />
    public partial class AddOrgs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Orgs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orgs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrgMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrgId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrgMembers_Orgs_OrgId",
                        column: x => x.OrgId,
                        principalTable: "Orgs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrgMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrgMembers_OrgId",
                table: "OrgMembers",
                column: "OrgId");

            migrationBuilder.CreateIndex(
                name: "IX_OrgMembers_UserId_OrgId",
                table: "OrgMembers",
                columns: new[] { "UserId", "OrgId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orgs_Name",
                table: "Orgs",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrgMembers");

            migrationBuilder.DropTable(
                name: "Orgs");
        }
    }
}
