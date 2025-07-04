import { Link } from 'react-router'
import { useSelector } from 'react-redux'
import {
  selectIsAuthenticated,
  signedOut,
} from '../../features/identity/identitySlice.ts'
import { useAppDispatch } from '../../stores/hooks.ts'

export const Header = () => {
  const isAuthenticated = useSelector(selectIsAuthenticated)
  const dispatch = useAppDispatch()

  function handleSignOut() {
    dispatch(signedOut())
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
                <Link
                  to="/signup"
                  className="px-4 py-2 text-sm font-bold text-lime-300 hover:rounded-md hover:bg-linear-to-bl hover:from-lime-300 hover:to-lime-400 hover:text-indigo-500"
                >
                  Sign Up
                </Link>
                <Link
                  to="/signin"
                  className="px-4 py-2 text-sm font-bold text-lime-300 hover:rounded-md hover:bg-linear-to-bl hover:from-lime-300 hover:to-lime-400 hover:text-indigo-500"
                >
                  Sign In
                </Link>
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
