using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LcmCrdt.Migrations
{
    /// <inheritdoc />
    public partial class AddCommitsOrderIndex : Migration
    {
        /// <summary>
    /// Name of the compound index over <c>(DateTime, Counter, Id)</c> on the <c>Commits</c> table.
    /// </summary>
    public const string CommitsOrderIndexName = "IX_Commits_DateTime_Counter_Id";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: CommitsOrderIndexName,
                table: "Commits",
                columns: ["DateTime", "Counter", "Id"],
                descending: [true, true, true]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: CommitsOrderIndexName, table: "Commits");
        }
    }
}
