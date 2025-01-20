using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LcmCrdt.Migrations
{
    /// <inheritdoc />
    public partial class AddOrdertoComplexFormComponents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Order",
                table: "ComplexFormComponents",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Order",
                table: "ComplexFormComponents");
        }
    }
}
