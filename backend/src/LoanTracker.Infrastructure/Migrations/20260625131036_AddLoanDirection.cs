using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanDirection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "direction",
                table: "loans",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Borrowed");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "direction",
                table: "loans");
        }
    }
}
