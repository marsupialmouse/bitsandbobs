import { Link, NavLink, useLocation, useNavigate } from 'react-router'
import { useSelector } from 'react-redux'
import { selectIsAuthenticated } from '../../features/usercontext/userContextSlice.ts'
import { useLogoutMutation } from '../../features/identity/identityApiSlice.ts'

export const Header = () => {
  const isAuthenticated = useSelector(selectIsAuthenticated)
  const [logout] = useLogoutMutation()
  const navigate = useNavigate()
  const location = useLocation()

  function handleSignOut() {
    const apiRequest = async () => {
      await logout().unwrap()
      const newLocation = { ...location, hash: 'signedOut' }
      await navigate(newLocation, { replace: true })
    }

    apiRequest().catch((e: unknown) => {
      console.log(e)
    })
  }

  return (
    <header className="fixed top-0 w-full bg-linear-to-bl from-indigo-400 to-indigo-700 shadow-md">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="flex h-16 items-center justify-between">
          <Link to="/" className="font-logo text-5xl font-bold text-lime-300">
            BITS&BOBS
          </Link>
          <div className="flex items-center space-x-4">
            {!isAuthenticated ? (
              <div className="flex items-center space-x-2">
                <NavLink
                  to="/register"
                  className="px-4 py-2 text-sm font-bold text-lime-300 hover:rounded-md hover:bg-linear-to-bl hover:from-lime-300 hover:to-lime-400 hover:text-indigo-500"
                >
                  Sign Up
                </NavLink>
                <NavLink
                  to="/login"
                  className="px-4 py-2 text-sm font-bold text-lime-300 hover:rounded-md hover:bg-linear-to-bl hover:from-lime-300 hover:to-lime-400 hover:text-indigo-500"
                >
                  Sign In
                </NavLink>
              </div>
            ) : (
              <div className="flex items-center space-x-2">
                <Link
                  to="/profile"
                  className="px-4 py-2 text-sm font-bold text-lime-300 hover:rounded-md hover:bg-lime-300 hover:text-indigo-500"
                >
                  Profile
                </Link>
                <button
                  onClick={handleSignOut}
                  className="px-4 py-2 text-sm font-bold text-lime-300 hover:rounded-md hover:bg-lime-300 hover:text-indigo-500"
                >
                  Sign Out
                </button>
              </div>
            )}
          </div>
        </div>
      </div>
    </header>
  )
}
