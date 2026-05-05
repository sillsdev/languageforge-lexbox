using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LcmCrdt.Migrations
{
    /// <inheritdoc />
    public partial class AddCommitsOrderIndex : Migration
    {
        // Compound index that matches DefaultOrder / WhereAfter / CurrentSnapshots CTE.
        // The CurrentSnapshots CTE joins Snapshots to Commits and orders by
        // (DateTime DESC, Counter DESC, Id DESC) inside a window function; without this
        // index SQLite does a full sort per bulk-sync, which added minutes to MarkDeleted cascades.
        // EF Core's HasIndex on complex-property members (HybridDateTime.DateTime, .Counter)
        // didn't generate a valid migration, so we create the index via raw SQL here.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS IX_Commits_DateTime_Counter_Id ON Commits (DateTime DESC, Counter DESC, Id DESC)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Commits_DateTime_Counter_Id");
        }
    }
}
