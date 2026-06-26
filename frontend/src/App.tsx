import { BrowserRouter, Route, Routes, Navigate } from 'react-router-dom'
import Layout from '@/components/layout/Layout'
import Dashboard from '@/pages/Dashboard'
import LenderList from '@/pages/Lenders/LenderList'
import LenderDetail from '@/pages/Lenders/LenderDetail'
import LoanList from '@/pages/Loans/LoanList'
import LoanDetail from '@/pages/Loans/LoanDetail'
import Reports from '@/pages/Reports/Reports'
import Account from '@/pages/Account'
import Login from '@/pages/Login'
import { Toaster } from '@/components/ui/toaster'
import { AuthProvider } from '@/contexts/AuthContext'
import ProtectedRoute from '@/components/auth/ProtectedRoute'

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route
            path="/"
            element={
              <ProtectedRoute>
                <Layout />
              </ProtectedRoute>
            }
          >
            <Route index element={<Navigate to="/dashboard" replace />} />
            <Route
              path="dashboard"
              element={<Dashboard title="Overall Dashboard" subtitle="All loans — borrowed and lent combined" />}
            />
            <Route
              path="loan-dashboard"
              element={<Dashboard direction="Borrowed" title="Loan Dashboard" subtitle="Money you borrowed (you owe)" />}
            />
            <Route
              path="lent-dashboard"
              element={<Dashboard direction="Lent" title="Lent Dashboard" subtitle="Money you lent out (owed to you)" />}
            />
            <Route path="lenders" element={<LenderList />} />
            <Route path="lenders/:id" element={<LenderDetail />} />
            <Route path="loans" element={<LoanList />} />
            <Route path="loans/:id" element={<LoanDetail />} />
            <Route path="reports" element={<Reports />} />
            <Route path="account" element={<Account />} />
          </Route>
        </Routes>
        <Toaster />
      </BrowserRouter>
    </AuthProvider>
  )
}
