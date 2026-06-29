using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBankingModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bank_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    account_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    account_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    bank = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bank_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bank_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    bank_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seq = table.Column<int>(type: "integer", nullable: false),
                    txn_date = table.Column<DateOnly>(type: "date", nullable: false),
                    value_date = table.Column<DateOnly>(type: "date", nullable: true),
                    narration = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    reference_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    withdrawal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    deposit = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    closing_balance = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    direction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    txn_group = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    payment_method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    merchant = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    dedup_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bank_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_bank_transactions_bank_accounts_bank_account_id",
                        column: x => x.bank_account_id,
                        principalTable: "bank_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bank_accounts_account_number",
                table: "bank_accounts",
                column: "account_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bank_transactions_bank_account_id_dedup_key",
                table: "bank_transactions",
                columns: new[] { "bank_account_id", "dedup_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bank_transactions_txn_date",
                table: "bank_transactions",
                column: "txn_date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bank_transactions");

            migrationBuilder.DropTable(
                name: "bank_accounts");
        }
    }
}
