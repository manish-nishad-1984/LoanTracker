import { Outlet } from 'react-router-dom'
import Sidebar from './Sidebar'
import Header from './Header'

export default function Layout() {
  return (
    <div className="min-h-screen bg-background">
      <Sidebar />
      <div className="lg:pl-60 print:pl-0">
        <Header />
        <main className="p-6 print:p-0">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
