import { Navigate, Route, Routes } from 'react-router-dom'

import PrivateRoute from './components/PrivateRoute.jsx'
import AppLayout from './layouts/AppLayout.jsx'
import AuthLayout from './layouts/AuthLayout.jsx'
import DashboardPage from './pages/DashboardPage.jsx'
import LoginPage from './pages/LoginPage.jsx'
import NotificationsPage from './pages/NotificationsPage.jsx'
import NotFoundPage from './pages/NotFoundPage.jsx'
import ReclamationCreatePage from './pages/ReclamationCreatePage.jsx'
import ReclamationDetailPage from './pages/ReclamationDetailPage.jsx'
import ReclamationsListPage from './pages/ReclamationsListPage.jsx'
import RegisterPage from './pages/RegisterPage.jsx'

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
