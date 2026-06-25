-- ============================================================
-- LoanTracker — PostgreSQL Schema
-- ============================================================

-- Enable UUID generation
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ============================================================
-- ENUM TYPES
-- ============================================================

CREATE TYPE lender_type AS ENUM (
    'person',
    'bank',
    'nbfc',
    'cooperative',
    'other'
);

CREATE TYPE interest_calculation_type AS ENUM (
    'simple',
    'compound',
    'flat',
    'reducing_balance',
    'none'
);

CREATE TYPE payment_frequency_type AS ENUM (
    'daily',
    'weekly',
    'fortnightly',
    'monthly',
    'quarterly',
    'half_yearly',
    'yearly',
    'lump_sum',
    'irregular'
);

CREATE TYPE loan_status AS ENUM (
    'active',
    'closed',
    'defaulted',
    'restructured',
    'written_off'
);

CREATE TYPE payment_mode AS ENUM (
    'cash',
    'bank_transfer',
    'upi',
    'cheque',
    'demand_draft',
    'online',
    'other'
);

-- ============================================================
-- LENDERS
-- ============================================================

CREATE TABLE lenders (
    id                  UUID            NOT NULL DEFAULT gen_random_uuid(),
    lender_type         lender_type     NOT NULL,
    name                VARCHAR(200)    NOT NULL,
    contact_name        VARCHAR(200),           -- for bank: branch manager or contact person
    phone               VARCHAR(20),
    email               VARCHAR(200),
    address             TEXT,
    bank_name           VARCHAR(200),           -- for bank lenders: formal bank name
    account_number      VARCHAR(50),            -- for bank lenders
    ifsc_code           VARCHAR(20),            -- for Indian bank lenders
    pan_number          VARCHAR(20),            -- tax tracking
    notes               TEXT,
    is_active           BOOLEAN         NOT NULL DEFAULT TRUE,
    created_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    deleted_at          TIMESTAMPTZ,

    CONSTRAINT pk_lenders PRIMARY KEY (id),
    CONSTRAINT chk_lenders_name_not_empty CHECK (TRIM(name) <> '')
);

CREATE INDEX idx_lenders_type        ON lenders (lender_type) WHERE deleted_at IS NULL;
CREATE INDEX idx_lenders_is_active   ON lenders (is_active)   WHERE deleted_at IS NULL;
CREATE INDEX idx_lenders_name        ON lenders (LOWER(name));
CREATE INDEX idx_lenders_deleted_at  ON lenders (deleted_at);

-- ============================================================
-- LOANS
-- ============================================================

CREATE TABLE loans (
    id                          UUID                        NOT NULL DEFAULT gen_random_uuid(),
    lender_id                   UUID                        NOT NULL,
    loan_number                 VARCHAR(100),               -- bank loan ref / informal identifier
    description                 VARCHAR(500),               -- human-readable purpose
    principal_amount            NUMERIC(18, 2)              NOT NULL,
    current_interest_rate       NUMERIC(8, 4)               NOT NULL DEFAULT 0,
    interest_calculation_type   interest_calculation_type   NOT NULL DEFAULT 'simple',
    payment_frequency           payment_frequency_type      NOT NULL DEFAULT 'monthly',
    is_emi_loan                 BOOLEAN                     NOT NULL DEFAULT FALSE,
    emi_amount                  NUMERIC(18, 2),
    loan_start_date             DATE                        NOT NULL,
    loan_end_date               DATE,                       -- expected/agreed end date (nullable for informal)
    status                      loan_status                 NOT NULL DEFAULT 'active',
    closure_date                DATE,                       -- actual closure date
    closure_notes               TEXT,
    notes                       TEXT,
    created_at                  TIMESTAMPTZ                 NOT NULL DEFAULT NOW(),
    updated_at                  TIMESTAMPTZ                 NOT NULL DEFAULT NOW(),
    deleted_at                  TIMESTAMPTZ,

    CONSTRAINT pk_loans                 PRIMARY KEY (id),
    CONSTRAINT fk_loans_lender          FOREIGN KEY (lender_id) REFERENCES lenders (id),
    CONSTRAINT chk_loan_principal_pos   CHECK (principal_amount > 0),
    CONSTRAINT chk_loan_rate_nn         CHECK (current_interest_rate >= 0),
    CONSTRAINT chk_loan_emi_amount      CHECK (
        is_emi_loan = FALSE OR (is_emi_loan = TRUE AND emi_amount IS NOT NULL AND emi_amount > 0)
    ),
    CONSTRAINT chk_loan_closure_date    CHECK (
        closure_date IS NULL OR closure_date >= loan_start_date
    ),
    CONSTRAINT chk_loan_end_date        CHECK (
        loan_end_date IS NULL OR loan_end_date >= loan_start_date
    )
);

CREATE UNIQUE INDEX idx_loans_loan_number ON loans (loan_number) WHERE loan_number IS NOT NULL AND deleted_at IS NULL;
CREATE INDEX idx_loans_lender_id         ON loans (lender_id);
CREATE INDEX idx_loans_status            ON loans (status) WHERE deleted_at IS NULL;
CREATE INDEX idx_loans_start_date        ON loans (loan_start_date);
CREATE INDEX idx_loans_deleted_at        ON loans (deleted_at);

-- ============================================================
-- LOAN INTEREST RATE HISTORY
-- ============================================================

CREATE TABLE loan_interest_rate_history (
    id              UUID            NOT NULL DEFAULT gen_random_uuid(),
    loan_id         UUID            NOT NULL,
    previous_rate   NUMERIC(8, 4)   NOT NULL,
    new_rate        NUMERIC(8, 4)   NOT NULL,
    effective_date  DATE            NOT NULL,
    reason          VARCHAR(500),
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    created_by      VARCHAR(200),   -- future auth user

    CONSTRAINT pk_loan_rate_history         PRIMARY KEY (id),
    CONSTRAINT fk_loan_rate_history_loan    FOREIGN KEY (loan_id) REFERENCES loans (id),
    CONSTRAINT chk_rate_history_prev_nn     CHECK (previous_rate >= 0),
    CONSTRAINT chk_rate_history_new_nn      CHECK (new_rate >= 0)
);

CREATE INDEX idx_rate_history_loan_id       ON loan_interest_rate_history (loan_id);
CREATE INDEX idx_rate_history_eff_date      ON loan_interest_rate_history (loan_id, effective_date DESC);

-- ============================================================
-- LOAN PAYMENTS
-- ============================================================

CREATE TABLE loan_payments (
    id                  UUID            NOT NULL DEFAULT gen_random_uuid(),
    loan_id             UUID            NOT NULL,
    payment_date        DATE            NOT NULL,
    total_amount        NUMERIC(18, 2)  NOT NULL,
    principal_amount    NUMERIC(18, 2)  NOT NULL DEFAULT 0,
    interest_amount     NUMERIC(18, 2)  NOT NULL DEFAULT 0,
    penalty_amount      NUMERIC(18, 2)  NOT NULL DEFAULT 0,
    payment_mode        payment_mode    NOT NULL DEFAULT 'cash',
    reference_number    VARCHAR(100),   -- bank txn ref, cheque no, UPI ref
    remarks             TEXT,
    is_deleted          BOOLEAN         NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    created_by          VARCHAR(200),   -- future auth user

    CONSTRAINT pk_loan_payments                 PRIMARY KEY (id),
    CONSTRAINT fk_loan_payments_loan            FOREIGN KEY (loan_id) REFERENCES loans (id),
    CONSTRAINT chk_payment_total_pos            CHECK (total_amount > 0),
    CONSTRAINT chk_payment_principal_nn         CHECK (principal_amount >= 0),
    CONSTRAINT chk_payment_interest_nn          CHECK (interest_amount >= 0),
    CONSTRAINT chk_payment_penalty_nn           CHECK (penalty_amount >= 0),
    CONSTRAINT chk_payment_amounts_sum          CHECK (
        principal_amount + interest_amount + penalty_amount = total_amount
    ),
    CONSTRAINT chk_payment_not_all_zero        CHECK (
        principal_amount > 0 OR interest_amount > 0 OR penalty_amount > 0
    )
);

CREATE INDEX idx_payments_loan_id       ON loan_payments (loan_id) WHERE is_deleted = FALSE;
CREATE INDEX idx_payments_date          ON loan_payments (payment_date DESC) WHERE is_deleted = FALSE;
CREATE INDEX idx_payments_loan_date     ON loan_payments (loan_id, payment_date DESC) WHERE is_deleted = FALSE;

-- ============================================================
-- LOAN DOCUMENTS (optional but included for production use)
-- ============================================================

CREATE TABLE loan_documents (
    id              UUID            NOT NULL DEFAULT gen_random_uuid(),
    loan_id         UUID            NOT NULL,
    document_name   VARCHAR(300)    NOT NULL,
    document_type   VARCHAR(100),   -- 'agreement', 'receipt', 'sanction_letter', 'noc', 'other'
    file_path       TEXT,           -- path in object storage
    file_size_bytes BIGINT,
    notes           TEXT,
    uploaded_at     TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    uploaded_by     VARCHAR(200),

    CONSTRAINT pk_loan_documents            PRIMARY KEY (id),
    CONSTRAINT fk_loan_documents_loan       FOREIGN KEY (loan_id) REFERENCES loans (id)
);

CREATE INDEX idx_documents_loan_id ON loan_documents (loan_id);

-- ============================================================
-- VIEWS
-- ============================================================

-- Per-loan summary with all computed financials
CREATE OR REPLACE VIEW v_loan_summary AS
SELECT
    l.id                                                                AS loan_id,
    l.lender_id,
    le.name                                                             AS lender_name,
    le.lender_type,
    l.loan_number,
    l.description,
    l.principal_amount                                                  AS original_principal,
    l.current_interest_rate,
    l.interest_calculation_type,
    l.payment_frequency,
    l.is_emi_loan,
    l.emi_amount,
    l.loan_start_date,
    l.loan_end_date,
    l.status,
    l.closure_date,
    l.created_at,
    COALESCE(SUM(p.principal_amount), 0)                               AS total_principal_paid,
    COALESCE(SUM(p.interest_amount),  0)                               AS total_interest_paid,
    COALESCE(SUM(p.penalty_amount),   0)                               AS total_penalty_paid,
    COALESCE(SUM(p.total_amount),     0)                               AS total_amount_paid,
    l.principal_amount - COALESCE(SUM(p.principal_amount), 0)          AS outstanding_principal,
    COUNT(p.id)                                                        AS payment_count,
    MAX(p.payment_date)                                                AS last_payment_date,
    MIN(p.payment_date)                                                AS first_payment_date
FROM loans l
JOIN lenders le ON le.id = l.lender_id AND le.deleted_at IS NULL
LEFT JOIN loan_payments p ON p.loan_id = l.id AND p.is_deleted = FALSE
WHERE l.deleted_at IS NULL
GROUP BY
    l.id, l.lender_id, le.name, le.lender_type,
    l.loan_number, l.description, l.principal_amount,
    l.current_interest_rate, l.interest_calculation_type,
    l.payment_frequency, l.is_emi_loan, l.emi_amount,
    l.loan_start_date, l.loan_end_date, l.status,
    l.closure_date, l.created_at;

-- Per-lender aggregated summary
CREATE OR REPLACE VIEW v_lender_summary AS
SELECT
    le.id                                               AS lender_id,
    le.name                                             AS lender_name,
    le.lender_type,
    le.is_active,
    COUNT(DISTINCT l.id)                                AS total_loans,
    COUNT(DISTINCT l.id) FILTER (WHERE l.status = 'active')   AS active_loans,
    COUNT(DISTINCT l.id) FILTER (WHERE l.status = 'closed')   AS closed_loans,
    COALESCE(SUM(l.principal_amount), 0)                AS total_borrowed,
    COALESCE(SUM(ls.total_principal_paid), 0)           AS total_principal_paid,
    COALESCE(SUM(ls.total_interest_paid), 0)            AS total_interest_paid,
    COALESCE(SUM(ls.outstanding_principal), 0)          AS total_outstanding,
    COALESCE(SUM(ls.total_amount_paid), 0)              AS total_amount_paid
FROM lenders le
LEFT JOIN loans l ON l.lender_id = le.id AND l.deleted_at IS NULL
LEFT JOIN v_loan_summary ls ON ls.loan_id = l.id
WHERE le.deleted_at IS NULL
GROUP BY le.id, le.name, le.lender_type, le.is_active;

-- Overall dashboard metrics
CREATE OR REPLACE VIEW v_dashboard_summary AS
SELECT
    COALESCE(SUM(principal_amount), 0)          AS total_borrowed,
    COALESCE(SUM(total_principal_paid), 0)      AS total_principal_paid,
    COALESCE(SUM(total_interest_paid), 0)       AS total_interest_paid,
    COALESCE(SUM(total_penalty_paid), 0)        AS total_penalty_paid,
    COALESCE(SUM(total_amount_paid), 0)         AS total_amount_paid,
    COALESCE(SUM(outstanding_principal), 0)     AS total_outstanding,
    COUNT(*) FILTER (WHERE status = 'active')   AS active_loans,
    COUNT(*) FILTER (WHERE status = 'closed')   AS closed_loans,
    COUNT(*) FILTER (WHERE status = 'defaulted') AS defaulted_loans,
    COUNT(*)                                    AS total_loans
FROM v_loan_summary;

-- Monthly payment analysis (rolling 24 months)
CREATE OR REPLACE VIEW v_monthly_payments AS
SELECT
    DATE_TRUNC('month', p.payment_date)         AS month,
    EXTRACT(YEAR  FROM p.payment_date)::INT     AS year,
    EXTRACT(MONTH FROM p.payment_date)::INT     AS month_num,
    COALESCE(SUM(p.principal_amount), 0)        AS principal_paid,
    COALESCE(SUM(p.interest_amount), 0)         AS interest_paid,
    COALESCE(SUM(p.penalty_amount), 0)          AS penalty_paid,
    COALESCE(SUM(p.total_amount), 0)            AS total_paid,
    COUNT(p.id)                                 AS payment_count
FROM loan_payments p
JOIN loans l ON l.id = p.loan_id AND l.deleted_at IS NULL
WHERE p.is_deleted = FALSE
GROUP BY DATE_TRUNC('month', p.payment_date),
         EXTRACT(YEAR FROM p.payment_date),
         EXTRACT(MONTH FROM p.payment_date)
ORDER BY month DESC;

-- ============================================================
-- TRIGGER: auto-update updated_at
-- ============================================================

CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_lenders_updated_at
    BEFORE UPDATE ON lenders
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_loans_updated_at
    BEFORE UPDATE ON loans
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_payments_updated_at
    BEFORE UPDATE ON loan_payments
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

-- ============================================================
-- TRIGGER: auto-close loan when outstanding = 0
-- ============================================================

CREATE OR REPLACE FUNCTION check_loan_closure()
RETURNS TRIGGER AS $$
DECLARE
    v_outstanding NUMERIC(18,2);
    v_status      loan_status;
BEGIN
    SELECT
        l.principal_amount - COALESCE(SUM(p.principal_amount), 0),
        l.status
    INTO v_outstanding, v_status
    FROM loans l
    LEFT JOIN loan_payments p ON p.loan_id = l.id AND p.is_deleted = FALSE
    WHERE l.id = NEW.loan_id
    GROUP BY l.principal_amount, l.status;

    IF v_outstanding <= 0 AND v_status = 'active' THEN
        UPDATE loans
        SET status = 'closed', closure_date = NEW.payment_date
        WHERE id = NEW.loan_id;
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_payment_check_closure
    AFTER INSERT OR UPDATE ON loan_payments
    FOR EACH ROW EXECUTE FUNCTION check_loan_closure();
