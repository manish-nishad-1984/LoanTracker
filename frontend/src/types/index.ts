// ─── Enums (mirror backend) ──────────────────────────────────────────────────

export type LenderType = 'Person' | 'Bank' | 'Nbfc' | 'Cooperative' | 'Other'

export type LoanDirection = 'Borrowed' | 'Lent'

export type InterestCalculationType =
  | 'Simple'
  | 'Compound'
  | 'Flat'
  | 'ReducingBalance'
  | 'None'

export type PaymentFrequency =
  | 'Daily'
  | 'Weekly'
  | 'Fortnightly'
  | 'Monthly'
  | 'Quarterly'
  | 'HalfYearly'
  | 'Yearly'
  | 'LumpSum'
  | 'Irregular'

export type LoanStatus =
  | 'Active'
  | 'Closed'
  | 'Defaulted'
  | 'Restructured'
  | 'WrittenOff'

export type PaymentMode =
  | 'Cash'
  | 'BankTransfer'
  | 'Upi'
  | 'Cheque'
  | 'DemandDraft'
  | 'Online'
  | 'Other'

// ─── Pagination ───────────────────────────────────────────────────────────────

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

// ─── Lenders ─────────────────────────────────────────────────────────────────

export interface LenderDto {
  id: string
  lenderType: LenderType
  lenderTypeName: string
  name: string
  contactName: string | null
  phone: string | null
  email: string | null
  address: string | null
  bankName: string | null
  accountNumber: string | null
  ifscCode: string | null
  panNumber: string | null
  notes: string | null
  isActive: boolean
  createdAt: string
  totalLoans: number
  activeLoans: number
  totalBorrowed: number
  totalOutstanding: number
}

export interface LenderListItemDto {
  id: string
  lenderType: LenderType
  lenderTypeName: string
  name: string
  phone: string | null
  email: string | null
  isActive: boolean
  activeLoans: number
  totalOutstanding: number
  createdAt: string
}

export interface CreateLenderRequest {
  lenderType: LenderType
  name: string
  contactName?: string
  phone?: string
  email?: string
  address?: string
  bankName?: string
  accountNumber?: string
  ifscCode?: string
  panNumber?: string
  notes?: string
}

export interface UpdateLenderRequest extends CreateLenderRequest {
  isActive: boolean
}

// ─── Loans ────────────────────────────────────────────────────────────────────

export interface LoanDto {
  id: string
  lenderId: string
  lenderName: string
  lenderType: LenderType
  direction: LoanDirection
  directionName: string
  loanNumber: string | null
  description: string | null
  principalAmount: number
  currentInterestRate: number
  interestCalculationType: InterestCalculationType
  interestCalculationTypeName: string
  paymentFrequency: PaymentFrequency
  paymentFrequencyName: string
  isEmiLoan: boolean
  emiAmount: number | null
  loanStartDate: string
  loanEndDate: string | null
  status: LoanStatus
  statusName: string
  closureDate: string | null
  closureNotes: string | null
  notes: string | null
  createdAt: string
  totalPrincipalPaid: number
  totalInterestPaid: number
  totalPenaltyPaid: number
  totalAmountPaid: number
  outstandingPrincipal: number
  interestAccruedToDate: number
  interestOutstanding: number
  paymentCount: number
  lastPaymentDate: string | null
}

export interface LoanListItemDto {
  id: string
  lenderId: string
  lenderName: string
  lenderType: LenderType
  direction: LoanDirection
  directionName: string
  loanNumber: string | null
  description: string | null
  principalAmount: number
  currentInterestRate: number
  status: LoanStatus
  statusName: string
  loanStartDate: string
  loanEndDate: string | null
  isEmiLoan: boolean
  outstandingPrincipal: number
  totalAmountPaid: number
  interestAccruedToDate: number
  totalInterestPaid: number
  lastPaymentDate: string | null
  createdAt: string
}

export interface CreateLoanRequest {
  lenderId: string
  direction: LoanDirection
  loanNumber?: string
  description?: string
  principalAmount: number
  currentInterestRate: number
  interestCalculationType: InterestCalculationType
  paymentFrequency: PaymentFrequency
  isEmiLoan: boolean
  emiAmount?: number
  loanStartDate: string
  loanEndDate?: string
  notes?: string
}

export interface UpdateLoanRequest {
  loanNumber?: string
  description?: string
  currentInterestRate: number
  interestCalculationType: InterestCalculationType
  paymentFrequency: PaymentFrequency
  isEmiLoan: boolean
  emiAmount?: number
  loanEndDate?: string
  notes?: string
}

export interface CloseLoanRequest {
  closureDate: string
  closureNotes?: string
}

export interface UpdateInterestRateRequest {
  newRate: number
  effectiveDate: string
  reason?: string
}

// ─── Payments ────────────────────────────────────────────────────────────────

export interface PaymentDto {
  id: string
  loanId: string
  loanDescription: string
  lenderName: string
  paymentDate: string
  totalAmount: number
  principalAmount: number
  interestAmount: number
  penaltyAmount: number
  paymentMode: PaymentMode
  paymentModeName: string
  referenceNumber: string | null
  remarks: string | null
  createdAt: string
}

export interface PaymentListItemDto extends PaymentDto {}

export interface CreatePaymentRequest {
  loanId: string
  paymentDate: string
  totalAmount: number
  principalAmount: number
  interestAmount: number
  penaltyAmount: number
  paymentMode: PaymentMode
  referenceNumber?: string
  remarks?: string
}

export interface UpdatePaymentRequest {
  paymentDate: string
  totalAmount: number
  principalAmount: number
  interestAmount: number
  penaltyAmount: number
  paymentMode: PaymentMode
  referenceNumber?: string
  remarks?: string
}

// ─── Dashboard ───────────────────────────────────────────────────────────────

export interface DashboardSummaryDto {
  totalBorrowed: number
  totalOutstanding: number
  totalPrincipalPaid: number
  totalInterestPaid: number
  totalInterestAccrued: number
  totalInterestOutstanding: number
  totalPenaltyPaid: number
  totalAmountPaid: number
  borrowedOutstanding: number
  lentOutstanding: number
  netPosition: number
  borrowedLoans: number
  lentLoans: number
  totalLoans: number
  activeLoans: number
  closedLoans: number
  defaultedLoans: number
  thisMonthPayment: number
  thisYearPayment: number
  lenderSummaries: LenderSummaryDto[]
  recentPayments: RecentPaymentDto[]
  monthlyTrend: MonthlyPaymentDto[]
  topOutstandingLoans: LoanOutstandingDto[]
}

export interface LenderSummaryDto {
  lenderId: string
  lenderName: string
  lenderType: string
  totalLoans: number
  activeLoans: number
  totalBorrowed: number
  totalOutstanding: number
  totalPrincipalPaid: number
  totalInterestPaid: number
  totalAmountPaid: number
  interestAccrued: number
  interestOutstanding: number
}

export interface MonthlyPaymentDto {
  year: number
  month: number
  monthName: string
  principalPaid: number
  interestPaid: number
  penaltyPaid: number
  totalPaid: number
  paymentCount: number
}

export interface RecentPaymentDto {
  id: string
  loanId: string
  loanDescription: string
  lenderName: string
  paymentDate: string
  totalAmount: number
  principalAmount: number
  interestAmount: number
  paymentModeName: string
}

export interface LoanOutstandingDto {
  loanId: string
  loanDescription: string
  lenderName: string
  originalPrincipal: number
  outstandingPrincipal: number
  currentInterestRate: number
  status: string
}

export interface YearlySummaryDto {
  year: number
  totalPaid: number
  principalPaid: number
  interestPaid: number
  paymentCount: number
}
