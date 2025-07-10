import { Navigate, Outlet, useLocation } from 'react-router'
import { useSelector } from 'react-redux'
import { selectIsAuthenticated } from '../features/usercontext/userContextSlice.ts'

export default function ProtectedRoute() {
  const location = useLocation()
  const isAuthenticated = useSelector(selectIsAuthenticated)

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />
  }

  return <Outlet />
}
