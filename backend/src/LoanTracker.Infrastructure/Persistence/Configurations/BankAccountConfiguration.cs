using LoanTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanTracker.Infrastructure.Persistence.Configurations;

public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.ToTable("bank_accounts");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.AccountNumber).HasColumnName("account_number").HasMaxLength(50).IsRequired();
        builder.Property(e => e.AccountName).HasColumnName("account_name").HasMaxLength(200);
        builder.Property(e => e.Bank).HasColumnName("bank").HasMaxLength(100);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(e => e.AccountNumber).IsUnique();
        builder.HasMany(e => e.Transactions).WithOne(e => e.Account)
            .HasForeignKey(e => e.BankAccountId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class BankTransactionConfiguration : IEntityTypeConfiguration<BankTransaction>
{
    public void Configure(EntityTypeBuilder<BankTransaction> builder)
    {
        builder.ToTable("bank_transactions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.BankAccountId).HasColumnName("bank_account_id").IsRequired();
        builder.Property(e => e.Seq).HasColumnName("seq");
        builder.Property(e => e.TxnDate).HasColumnName("txn_date");
        builder.Property(e => e.ValueDate).HasColumnName("value_date");
        builder.Property(e => e.Narration).HasColumnName("narration").HasMaxLength(500);
        builder.Property(e => e.ReferenceNo).HasColumnName("reference_no").HasMaxLength(100);
        builder.Property(e => e.Withdrawal).HasColumnName("withdrawal").HasColumnType("numeric(18,2)");
        builder.Property(e => e.Deposit).HasColumnName("deposit").HasColumnType("numeric(18,2)");
        builder.Property(e => e.ClosingBalance).HasColumnName("closing_balance").HasColumnType("numeric(18,2)");
        builder.Property(e => e.Direction).HasColumnName("direction").HasMaxLength(10);
        builder.Property(e => e.Group).HasColumnName("txn_group").HasMaxLength(20);
        builder.Property(e => e.PaymentMethod).HasColumnName("payment_method").HasMaxLength(20);
        builder.Property(e => e.Category).HasColumnName("category").HasMaxLength(50);
        builder.Property(e => e.Merchant).HasColumnName("merchant").HasMaxLength(200);
        builder.Property(e => e.DedupKey).HasColumnName("dedup_key").HasMaxLength(200);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(e => new { e.BankAccountId, e.DedupKey }).IsUnique();
        builder.HasIndex(e => e.TxnDate);
    }
}
