# LoanTracker — Simple Project Documentation

A friendly, plain-English guide to what this project is, how it's built, and how it
all fits together. No heavy jargon — diagrams included.

---

## 1. What is LoanTracker?

LoanTracker is a website to **keep track of loans** — money you **borrow** from people
or banks, and money you **lend** to friends or relatives.

It answers everyday questions like:
- How much do I still owe? How much is owed to me?
- How much interest has built up so far?
- Who did I borrow from / lend to, and when?
- What payments have I made or received?

Think of it as a **smart digital notebook** for loans, with charts and totals done for you.

Live site: **https://motiwala.pratishthabridal.com**

---

## 2. The Big Picture (Architecture)

The app has **three main parts** that talk to each other:

```
        ┌─────────────────────────────────────────────────────────────┐
        │                        YOUR BROWSER                          │
        │                                                              │
        │   ┌──────────────────────────────────────────────────────┐  │
        │   │   FRONTEND  (React website — what you see & click)     │  │
        │   │   Pages: Login, Dashboard, Lenders, Loans, Reports     │  │
        │   └──────────────────────────────────────────────────────┘  │
        └───────────────────────────┬─────────────────────────────────┘
                                    │  asks for / sends data (HTTPS)
                                    │  e.g. "give me the dashboard"
                                    ▼
        ┌─────────────────────────────────────────────────────────────┐
        │   BACKEND  (the "brain" — a .NET program)                    │
        │   - Checks your login (security)                             │
        │   - Does the math (interest, totals, balances)              │
        │   - Decides the rules (can't overpay, auto-close, etc.)     │
        └───────────────────────────┬─────────────────────────────────┘
                                    │  reads / writes records
                                    ▼
        ┌─────────────────────────────────────────────────────────────┐
        │   DATABASE  (PostgreSQL — the "filing cabinet")             │
        │   Stores: users, lenders, loans, payments, rate history    │
        └─────────────────────────────────────────────────────────────┘
```

**Simple analogy — a restaurant:**
- **Frontend** = the dining area & menu (what the customer sees).
- **Backend** = the kitchen (does the actual work, follows recipes/rules).
- **Database** = the pantry/fridge (where all ingredients/records are stored).

You (the customer) never go into the kitchen or pantry directly — you talk to the
waiter (frontend), who passes orders to the kitchen (backend), which uses the pantry
(database).

---

## 3. What happens when you use it? (Data Flow)

Example: **You open the Dashboard.**

```
  1. You open the site ─────────────►  Frontend loads in your browser
                                              │
  2. Frontend asks:  "GET /api/dashboard"     │  (with your login token)
                                              ▼
  3. Backend checks your token ──► valid? ──► YES
                                              │
  4. Backend asks the database for all loans & payments
                                              ▼
  5. Backend calculates: total owed, interest, charts...
                                              │
  6. Backend sends the numbers back ──────────┘
                                              ▼
  7. Frontend draws the cards & charts you see on screen
```

Every action (add a lender, record a payment, etc.) follows this same pattern:
**Browser → Backend → Database → back to Browser.**

---

## 4. The Main Ideas (Business Concepts)

```
   LENDER  ──────────►  LOAN  ──────────►  PAYMENTS
 (a person or bank)   (the money +       (each repayment,
                       its terms)          split into parts)
```

### Lender
A person or a bank you deal with. Has a name, phone, type (Person/Bank), etc.

### Loan
The actual money and its rules:
- **Principal** = the amount of money.
- **Interest rate** = the % charged.
- **Direction** = **Borrowed** (you owe them) or **Lent** (they owe you).
- **Start date**, payment frequency, EMI or not, etc.

### Payment
Each time money changes hands. A payment can be split into:
- **Principal** (paying back the actual loan amount)
- **Interest** (the cost of borrowing)
- **Penalty** (a late fee, if any)

This flexibility is the heart of the app — a single payment can be **interest-only**,
**principal-only**, or **both mixed together**.

```
   Example payment of ₹21,500:
   ┌───────────────────────────────────────────┐
   │  Principal: ₹20,000  +  Interest: ₹1,500   │  = ₹21,500 total
   └───────────────────────────────────────────┘
```

### Outstanding & Interest (the key numbers)
- **Outstanding principal** = Loan amount − all principal paid. (What's still owed.)
- **Interest accrued to date** = how much interest has *built up* from the start date
  until today (calculated automatically on the shrinking balance).
- **Interest paid** = how much interest you've actually paid.
- **Interest outstanding** = accrued − paid (what's still pending).

---

## 5. The Screens (Pages)

```
  ┌────────────┐   ┌─────────────┐   ┌──────────┐   ┌──────────┐   ┌──────────┐
  │   LOGIN    │   │  DASHBOARD  │   │ LENDERS  │   │  LOANS   │   │ REPORTS  │
  └────────────┘   └─────────────┘   └──────────┘   └──────────┘   └──────────┘
   Sign in with     Totals, charts,   Add/edit      Add loans,      Yearly &
   username +       "I owe" vs        people &      record          monthly
   password         "owed to me"      banks         payments,       breakdowns,
                                                    see history     lender-wise
```

Plus an **Account Settings** page (click your name, top-right) to change your username
or password.

---

## 6. How Login / Security Works

```
  You type username + password
            │
            ▼
   Backend checks them against the database
            │
       correct? ──► YES ──► Backend gives you a "token"
            │                 (a digital wristband, valid for a while)
            ▼
   Browser keeps the token and shows it with every request
            │
            ▼
   Backend only answers if the token is valid
   (no token = "401 Not Allowed")
```

- Passwords are **never stored as plain text** — they're scrambled (hashed) so even we
  can't read them.
- The "token" is a **JWT** (a signed digital pass). If it expires or is missing, you're
  sent back to the login page.

---

## 7. The Technology (in plain terms)

| Part | Technology | What it is, simply |
|------|-----------|--------------------|
| Frontend | **React + TypeScript** | Builds the interactive website you click on |
| UI style | **Tailwind + shadcn/ui** | Pre-made, good-looking buttons/cards/forms |
| Charts | **Recharts** | Draws the bar & pie charts |
| Backend | **.NET 8 (C#)** | The brain that does logic and math |
| Database access | **Entity Framework** | Lets the backend talk to the database in C# |
| Database | **PostgreSQL** | Reliable, free database that stores everything |
| Login | **JWT tokens** | Secure digital passes for staying logged in |

---

## 8. How the Code is Organized (Folders)

```
LoanTracker/
│
├── backend/                  ← the "brain" (.NET / C#)
│   └── src/
│       ├── LoanTracker.Domain          ← the core ideas (Lender, Loan, Payment, User)
│       ├── LoanTracker.Application     ← rules, calculations, what each action does
│       ├── LoanTracker.Infrastructure  ← talks to the database; does the real work
│       └── LoanTracker.Api             ← the doorway the frontend talks to (endpoints)
│
├── frontend/                 ← the website (React)
│   └── src/
│       ├── pages/            ← each screen (Dashboard, Loans, Login, Account...)
│       ├── components/       ← reusable pieces (cards, buttons, layout)
│       ├── api/              ← code that calls the backend
│       └── hooks/            ← fetches & caches data from the backend
│
├── database/                 ← reference SQL (schema + sample data)
├── scripts/deploy.sh         ← one command to publish updates to the live server
└── DEPLOY.md / README.md     ← setup & deployment guides
```

**Why split the backend into 4 parts?** It's called *clean architecture* — each layer
has one job, so the code stays tidy and easy to change. Like a kitchen with separate
stations: prep, cooking, plating, and serving.

---

## 9. The Database Tables (the filing cabinet drawers)

```
  users                 → people who can log in (username, scrambled password)
  lenders               → the persons/banks you deal with
  loans                 → each loan (amount, rate, borrowed/lent, dates, status)
  loan_payments         → every payment (split into principal/interest/penalty)
  loan_interest_rate_history → record of any interest-rate changes over time
  loan_documents        → optional attached files (agreements, receipts)
```

They're linked like this:

```
  users (you log in)

  lenders ──< loans ──< loan_payments
                 │
                 └──< loan_interest_rate_history

  ( "──<" means "one has many" :  one lender has many loans,
                                  one loan has many payments )
```

---

## 10. Where It Lives (Deployment)

The app runs on a **Hostinger VPS** (a rented online computer) shared with other
websites. Here's how a visitor reaches it safely:

```
   Visitor's browser
        │   https://motiwala.pratishthabridal.com
        ▼
   ┌─────────────────────────────────────────────┐
   │  THE SERVER (Ubuntu Linux @ 213.210.37.67)  │
   │                                             │
   │   nginx  ──────────────────────────────┐    │   nginx = the receptionist:
   │   (front door, handles HTTPS/SSL)       │    │   greets visitors, shows the
   │      │                                  │    │   website, and forwards any
   │      ├──► serves the React website      │    │   "/api" requests to the brain
   │      │     (static files)               │    │
   │      │                                  │    │
   │      └──► forwards /api/* to ──► .NET API│    │
   │                                  (port   │    │
   │                                   5090)  │    │
   │                                    │     │    │
   │                                    ▼     │    │
   │                              PostgreSQL  │    │
   │                              (database)  │    │
   └─────────────────────────────────────────┘    │
```

- **nginx** = receptionist at the front door (handles the secure `https://` lock 🔒).
- **.NET API** = the brain, running quietly in the background as a service.
- **PostgreSQL** = the filing cabinet.
- A **firewall** only opens the doors that should be open (ports 80, 443, SSH).

### Updating the live site
Whenever the code changes, one command rebuilds and publishes it:

```
   bash scripts/deploy.sh
        │
        ├─ builds the backend + frontend on your PC
        ├─ uploads them to the server
        └─ restarts the app  →  new version is live
```

---

## 11. Key Rules the App Enforces (so data stays correct)

- A payment's parts must add up: **principal + interest + penalty = total**.
- You **can't pay more principal than what's owed**.
- When the balance reaches zero, the loan **auto-closes**.
- A **closed loan** won't accept new payments.
- Deleting/editing a payment **recalculates** all the totals automatically.
- Nothing is truly deleted — it's hidden (**soft delete**) so history is preserved.

---

## 12. Quick Glossary

| Term | Plain meaning |
|------|---------------|
| **Principal** | The actual loan amount (not counting interest) |
| **Interest** | The extra cost charged for borrowing |
| **Outstanding** | How much is still owed |
| **Accrued interest** | Interest that has built up so far (whether paid or not) |
| **Borrowed** | Money you took — *you owe* |
| **Lent** | Money you gave out — *owed to you* |
| **EMI** | A fixed monthly installment |
| **API** | The "doorway" the website uses to talk to the brain |
| **JWT / token** | A secure digital pass that keeps you logged in |
| **nginx** | The front-door software that serves the site & HTTPS |
| **Deploy** | Publish the latest version to the live server |

---

## 13. One-Page Summary

```
  WHAT:   A website to track loans you borrow and lend, with interest.

  PARTS:  Browser (React)  →  Backend brain (.NET)  →  Database (PostgreSQL)

  DOES:   • Add lenders (people/banks)
          • Add loans (borrowed or lent)
          • Record payments (principal / interest / mixed)
          • Auto-calculate balances, interest, and dashboards
          • Secure login per user

  LIVE:   https://motiwala.pratishthabridal.com  (login required)

  UPDATE: bash scripts/deploy.sh
```

That's the whole project in simple terms. 🎉
