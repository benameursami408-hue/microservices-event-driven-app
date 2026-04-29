import { Navigate, Route, Routes } from 'react-router-dom'

import PrivateRoute from './components/PrivateRoute.jsx'
import AppLayout from './layouts/AppLayout.jsx'
import AuthLayout from './layouts/AuthLayout.jsx'
import DashboardPage from './pages/DashboardPage.jsx'
import LoginPage from './pages/LoginPage.jsx'
import NotificationsPage from './pages/NotificationsPage.jsx'
import NotFoundPage from './pages/NotFoundPage.jsx'
import AdminDashboardPage from './pages/AdminDashboardPage.jsx'
import AdminSavPage from './pages/AdminSavPage.jsx'
import AdminStPage from './pages/AdminStPage.jsx'
import AdminUsersPage from './pages/AdminUsersPage.jsx'
import PlanningBoardPage from './pages/PlanningBoardPage.jsx'
import ReclamationCreatePage from './pages/ReclamationCreatePage.jsx'
import ReclamationDetailPage from './pages/ReclamationDetailPage.jsx'
import ReclamationsListPage from './pages/ReclamationsListPage.jsx'
import RegisterPage from './pages/RegisterPage.jsx'
import TechnicianAgendaPage from './pages/TechnicianAgendaPage.jsx'

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/app" replace />} />

      <Route element={<AuthLayout />}>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
      </Route>

      <Route element={<PrivateRoute />}>
        <Route element={<AppLayout />}>
          <Route path="/app" element={<DashboardPage />} />
          <Route path="/app/admin" element={<AdminDashboardPage />} />
          <Route path="/app/admin/users" element={<AdminUsersPage />} />
          <Route path="/app/admin/sav" element={<AdminSavPage />} />
          <Route path="/app/admin/st" element={<AdminStPage />} />
          <Route path="/app/planning" element={<PlanningBoardPage />} />
          <Route path="/app/interventions" element={<TechnicianAgendaPage />} />
          <Route path="/app/reclamations" element={<ReclamationsListPage />} />
          <Route path="/app/reclamations/new" element={<ReclamationCreatePage />} />
          <Route path="/app/reclamations/:id" element={<ReclamationDetailPage />} />
          <Route path="/app/notifications" element={<NotificationsPage />} />
        </Route>
      </Route>

      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  )
}
