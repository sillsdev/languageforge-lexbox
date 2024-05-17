using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LexData.Migrations
{
    /// <inheritdoc />
    public partial class AddCrdtCommits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CrdtCommits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    HybridDateTime_Counter = table.Column<long>(type: "bigint", nullable: false),
                    HybridDateTime_DateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Hash = table.Column<string>(type: "text", nullable: false),
                    ParentHash = table.Column<string>(type: "text", nullable: false),
                    Metadata = table.Column<string>(type: "text", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeEntities = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrdtCommits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrdtCommits_FlexProjectMetadata_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "FlexProjectMetadata",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CrdtCommits_ProjectId",
                table: "CrdtCommits",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrdtCommits");
        }
    }
}
