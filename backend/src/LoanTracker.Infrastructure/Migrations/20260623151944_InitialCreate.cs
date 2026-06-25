using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lenders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    lender_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contact_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    bank_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    account_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ifsc_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    pan_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lenders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "loans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    lender_id = table.Column<Guid>(type: "uuid", nullable: false),
                    loan_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    principal_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    current_interest_rate = table.Column<decimal>(type: "numeric(8,4)", nullable: false, defaultValue: 0m),
                    interest_calculation_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    payment_frequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_emi_loan = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    emi_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    loan_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    loan_end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    closure_date = table.Column<DateOnly>(type: "date", nullable: true),
                    closure_notes = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loans", x => x.id);
                    table.ForeignKey(
                        name: "FK_loans_lenders_lender_id",
                        column: x => x.lender_id,
                        principalTable: "lenders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "loan_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    loan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    document_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    file_path = table.Column<string>(type: "text", nullable: true),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    uploaded_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loan_documents", x => x.id);
                    table.ForeignKey(
                        name: "FK_loan_documents_loans_loan_id",
                        column: x => x.loan_id,
                        principalTable: "loans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "loan_interest_rate_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    loan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_rate = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    new_rate = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    effective_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loan_interest_rate_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_loan_interest_rate_history_loans_loan_id",
                        column: x => x.loan_id,
                        principalTable: "loans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "loan_payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    loan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    principal_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    interest_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    penalty_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    payment_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reference_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    remarks = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loan_payments", x => x.id);
                    table.ForeignKey(
                        name: "FK_loan_payments_loans_loan_id",
                        column: x => x.loan_id,
                        principalTable: "loans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lenders_is_active",
                table: "lenders",
                column: "is_active",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_lenders_lender_type",
                table: "lenders",
                column: "lender_type",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_loan_documents_loan_id",
                table: "loan_documents",
                column: "loan_id");

            migrationBuilder.CreateIndex(
                name: "IX_loan_interest_rate_history_loan_id_effective_date",
                table: "loan_interest_rate_history",
                columns: new[] { "loan_id", "effective_date" });

            migrationBuilder.CreateIndex(
                name: "IX_loan_payments_loan_id",
                table: "loan_payments",
                column: "loan_id",
                filter: "is_deleted = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_loan_payments_payment_date",
                table: "loan_payments",
                column: "payment_date",
                filter: "is_deleted = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_loans_lender_id",
                table: "loans",
                column: "lender_id");

            migrationBuilder.CreateIndex(
                name: "IX_loans_loan_number",
                table: "loans",
                column: "loan_number",
                unique: true,
                filter: "loan_number IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_loans_status",
                table: "loans",
                column: "status",
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "loan_documents");

            migrationBuilder.DropTable(
                name: "loan_interest_rate_history");

            migrationBuilder.DropTable(
                name: "loan_payments");

            migrationBuilder.DropTable(
                name: "loans");

            migrationBuilder.DropTable(
                name: "lenders");
        }
    }
}
