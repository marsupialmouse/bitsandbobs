import { Navigate, Outlet, useLocation } from 'react-router'
import { useSelector } from 'react-redux'
import { selectIsAuthenticated } from '../features/usercontext/userContextSlice.ts'

export default function ProtectedRoute() {
  const location = useLocation()
  const isAuthenticated = useSelector(selectIsAuthenticated)

  if (!isAuthenticated) {
    // If someone signs out on a protected route we should navigate home, not to the login page
    const path = location.hash === '#signedOut' ? '/' : '/login'
    return <Navigate to={path} state={{ from: location }} replace />
  }

  return <Outlet />
}
