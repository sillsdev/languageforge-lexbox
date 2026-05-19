using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LcmCrdt.Migrations
{
    /// <inheritdoc />
    // Backs harmony's default commit ordering. Hand-written because the index spans a
    // ComplexProperty + regular property (efcore#11336). Perhaps move to harmony's fluent API
    // via EFCore.ComplexIndexes when we're on .NET 10.
    public partial class AddCommitsOrderIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Commits_DateTime_Counter_Id",
                table: "Commits",
                columns: ["DateTime", "Counter", "Id"],
                descending: [true, true, true]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Commits_DateTime_Counter_Id", table: "Commits");
        }
    }
}
