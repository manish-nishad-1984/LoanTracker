-- ============================================================
-- LoanTracker — Seed Data
-- ============================================================

-- ============================================================
-- LENDERS
-- ============================================================

INSERT INTO lenders (id, lender_type, name, contact_name, phone, email, address, notes)
VALUES
(
    '11111111-0000-0000-0000-000000000001',
    'person',
    'Rajesh Sharma',
    NULL,
    '+91-98765-43210',
    'rajesh.sharma@gmail.com',
    'B-12, Vastrapur, Ahmedabad, Gujarat - 380015',
    'Family friend. Lends money informally at low interest.'
),
(
    '11111111-0000-0000-0000-000000000002',
    'person',
    'Priya Mehta',
    NULL,
    '+91-91234-56789',
    'priya.mehta@yahoo.com',
    'C-45, Satellite, Ahmedabad, Gujarat - 380015',
    'Colleague. Short-term lending only.'
);

INSERT INTO lenders (id, lender_type, name, contact_name, phone, email, address, bank_name, account_number, ifsc_code, notes)
VALUES
(
    '11111111-0000-0000-0000-000000000003',
    'bank',
    'HDFC Bank — Home Loan',
    'Amit Joshi (Relationship Manager)',
    '+91-79-4000-1111',
    'homeloan.ahmedabad@hdfcbank.com',
    'SG Highway Branch, Ahmedabad, Gujarat',
    'HDFC Bank Ltd',
    'HDFC00123456',
    'HDFC0001234',
    'Home loan account. Fixed-rate product.'
),
(
    '11111111-0000-0000-0000-000000000004',
    'bank',
    'SBI — Personal Loan',
    'Branch Manager',
    '+91-79-2656-7890',
    'sbi.navrangpura@sbi.co.in',
    'Navrangpura Branch, Ahmedabad, Gujarat',
    'State Bank of India',
    'SBI987654321',
    'SBIN0050123',
    'SBI personal loan — reducing balance interest.'
);

-- ============================================================
-- LOANS
-- ============================================================

-- Loan 1: Personal loan from Rajesh (simple interest, irregular)
INSERT INTO loans (id, lender_id, loan_number, description, principal_amount, current_interest_rate,
                   interest_calculation_type, payment_frequency, is_emi_loan, loan_start_date, loan_end_date, status, notes)
VALUES (
    '22222222-0000-0000-0000-000000000001',
    '11111111-0000-0000-0000-000000000001',
    'RAJESH/2023/01',
    'Emergency home repair loan from Rajesh bhai',
    200000.00,
    9.0000,
    'simple',
    'irregular',
    FALSE,
    '2023-06-01',
    NULL,
    'active',
    'Interest to be paid monthly; principal whenever possible.'
);

-- Loan 2: Small personal loan from Priya (zero interest)
INSERT INTO loans (id, lender_id, loan_number, description, principal_amount, current_interest_rate,
                   interest_calculation_type, payment_frequency, is_emi_loan, loan_start_date, loan_end_date, status, notes)
VALUES (
    '22222222-0000-0000-0000-000000000002',
    '11111111-0000-0000-0000-000000000002',
    'PRIYA/2024/01',
    'Short-term loan from Priya — office laptop',
    50000.00,
    0.0000,
    'none',
    'lump_sum',
    FALSE,
    '2024-01-15',
    '2024-07-15',
    'closed',
    'Zero interest friendly loan. Closed on time.'
);

-- Loan 3: HDFC Home Loan (reducing balance EMI)
INSERT INTO loans (id, lender_id, loan_number, description, principal_amount, current_interest_rate,
                   interest_calculation_type, payment_frequency, is_emi_loan, emi_amount,
                   loan_start_date, loan_end_date, status, notes)
VALUES (
    '22222222-0000-0000-0000-000000000003',
    '11111111-0000-0000-0000-000000000003',
    'HDFC/HL/AHM/2022/5678',
    'HDFC Home Loan — 3BHK apartment purchase',
    3500000.00,
    8.5000,
    'reducing_balance',
    'monthly',
    TRUE,
    32000.00,
    '2022-03-01',
    '2042-03-01',
    'active',
    '20-year home loan. EMI auto-debit on 5th of each month.'
);

-- Loan 4: SBI Personal Loan (flat rate EMI)
INSERT INTO loans (id, lender_id, loan_number, description, principal_amount, current_interest_rate,
                   interest_calculation_type, payment_frequency, is_emi_loan, emi_amount,
                   loan_start_date, loan_end_date, status, notes)
VALUES (
    '22222222-0000-0000-0000-000000000004',
    '11111111-0000-0000-0000-000000000004',
    'SBI/PL/2024/9012',
    'SBI Personal Loan — vehicle purchase',
    400000.00,
    12.0000,
    'reducing_balance',
    'monthly',
    TRUE,
    8900.00,
    '2024-03-01',
    '2028-03-01',
    'active',
    '4-year personal loan. EMI on 1st of each month.'
);

-- ============================================================
-- LOAN INTEREST RATE HISTORY
-- (HDFC rate changed from 8.75% to 8.5% after RBI cut)
-- ============================================================

INSERT INTO loan_interest_rate_history (loan_id, previous_rate, new_rate, effective_date, reason)
VALUES (
    '22222222-0000-0000-0000-000000000003',
    8.7500,
    8.5000,
    '2023-10-01',
    'RBI repo rate cut — HDFC passed on benefit to floating rate borrowers.'
);

-- ============================================================
-- LOAN PAYMENTS
-- ============================================================

-- Rajesh loan payments (Loan 1)
INSERT INTO loan_payments (loan_id, payment_date, total_amount, principal_amount, interest_amount, penalty_amount, payment_mode, remarks)
VALUES
-- Interest-only payment
('22222222-0000-0000-0000-000000000001', '2023-07-01', 1500.00, 0.00, 1500.00, 0.00, 'cash',
 'July interest @ 9% p.a. on 2L = ~1500/month'),

-- Interest-only payment
('22222222-0000-0000-0000-000000000001', '2023-08-01', 1500.00, 0.00, 1500.00, 0.00, 'cash',
 'August interest'),

-- Mixed: principal + interest
('22222222-0000-0000-0000-000000000001', '2023-09-01', 21500.00, 20000.00, 1500.00, 0.00, 'bank_transfer',
 'September — paid 20k principal + interest'),

-- Interest on remaining 1.8L (~1350)
('22222222-0000-0000-0000-000000000001', '2023-10-03', 1350.00, 0.00, 1350.00, 0.00, 'upi',
 'October interest on 180000 outstanding'),

-- Principal-only
('22222222-0000-0000-0000-000000000001', '2023-11-15', 30000.00, 30000.00, 0.00, 0.00, 'bank_transfer',
 'Principal repayment only'),

-- Mixed payment
('22222222-0000-0000-0000-000000000001', '2024-01-01', 11125.00, 10000.00, 1125.00, 0.00, 'cash',
 'Jan — 10k principal + interest on 150k'),

-- Interest-only
('22222222-0000-0000-0000-000000000001', '2024-03-01', 1050.00, 0.00, 1050.00, 0.00, 'upi',
 'March interest on 140k outstanding'),

-- Principal payment
('22222222-0000-0000-0000-000000000001', '2024-06-01', 40000.00, 40000.00, 0.00, 0.00, 'bank_transfer',
 'Large principal payment — bonus received');

-- Priya loan (Loan 2) — fully closed
INSERT INTO loan_payments (loan_id, payment_date, total_amount, principal_amount, interest_amount, payment_mode, remarks)
VALUES
('22222222-0000-0000-0000-000000000002', '2024-04-10', 25000.00, 25000.00, 0.00, 'bank_transfer', 'First partial repayment'),
('22222222-0000-0000-0000-000000000002', '2024-07-14', 25000.00, 25000.00, 0.00, 'upi', 'Final repayment — loan closed');

-- HDFC Home Loan payments (Loan 3) — 6 months of EMI
INSERT INTO loan_payments (loan_id, payment_date, total_amount, principal_amount, interest_amount, payment_mode, reference_number, remarks)
VALUES
('22222222-0000-0000-0000-000000000003', '2024-10-05', 32000.00, 7258.00, 24742.00, 0.00, 'bank_transfer', 'HDFC10OCT24', 'October EMI — auto-debit'),
('22222222-0000-0000-0000-000000000003', '2024-11-05', 32000.00, 7310.00, 24690.00, 0.00, 'bank_transfer', 'HDFC10NOV24', 'November EMI'),
('22222222-0000-0000-0000-000000000003', '2024-12-05', 32000.00, 7363.00, 24637.00, 0.00, 'bank_transfer', 'HDFC10DEC24', 'December EMI'),
('22222222-0000-0000-0000-000000000003', '2025-01-05', 32000.00, 7415.00, 24585.00, 0.00, 'bank_transfer', 'HDFC10JAN25', 'January EMI'),
('22222222-0000-0000-0000-000000000003', '2025-02-05', 32000.00, 7468.00, 24532.00, 0.00, 'bank_transfer', 'HDFC10FEB25', 'February EMI'),
('22222222-0000-0000-0000-000000000003', '2025-03-05', 32000.00, 7521.00, 24479.00, 0.00, 'bank_transfer', 'HDFC10MAR25', 'March EMI');

-- SBI Personal Loan payments (Loan 4)
INSERT INTO loan_payments (loan_id, payment_date, total_amount, principal_amount, interest_amount, payment_mode, reference_number, remarks)
VALUES
('22222222-0000-0000-0000-000000000004', '2024-04-01', 8900.00, 4900.00, 4000.00, 0.00, 'bank_transfer', 'SBI01APR24', 'April EMI'),
('22222222-0000-0000-0000-000000000004', '2024-05-01', 8900.00, 4949.00, 3951.00, 0.00, 'bank_transfer', 'SBI01MAY24', 'May EMI'),
('22222222-0000-0000-0000-000000000004', '2024-06-01', 8900.00, 4998.00, 3902.00, 0.00, 'bank_transfer', 'SBI01JUN24', 'June EMI'),
('22222222-0000-0000-0000-000000000004', '2024-07-01', 8900.00, 5048.00, 3852.00, 0.00, 'bank_transfer', 'SBI01JUL24', 'July EMI'),
('22222222-0000-0000-0000-000000000004', '2024-08-01', 8900.00, 5099.00, 3801.00, 0.00, 'bank_transfer', 'SBI01AUG24', 'August EMI');
