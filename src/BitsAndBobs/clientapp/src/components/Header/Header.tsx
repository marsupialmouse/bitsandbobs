import { Link, NavLink, useLocation, useNavigate } from 'react-router'
import { useSelector } from 'react-redux'
import {
  selectCurrentEmailAddress,
  selectIsAuthenticated,
} from '../../features/usercontext/userContextSlice.ts'
import { useLogoutMutation } from '../../features/identity/identityApiSlice.ts'

export const Header = () => {
  const isAuthenticated = useSelector(selectIsAuthenticated)
  const emailAddress = useSelector(selectCurrentEmailAddress)
  const [logout] = useLogoutMutation()
  const navigate = useNavigate()
  const location = useLocation()

  function handleSignOut(e: React.MouseEvent<HTMLAnchorElement>) {
    e.preventDefault()
    e.stopPropagation()

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
            <Link
              to="/auctions/create"
              className="px-4 py-2 text-sm font-bold text-lime-300 hover:rounded-md hover:bg-linear-to-bl hover:from-lime-300 hover:to-lime-400 hover:text-indigo-500"
            >
              Sell Your Bits & Bobs
            </Link>
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
              <div className="flex">
                <div className="group relative m-0">
                  <button className="m-0 p-1 text-lime-300 group-hover:rounded-t-md group-hover:bg-lime-300 group-hover:text-indigo-500">
                    <svg
                      className="h-10 w-10 fill-current stroke-current"
                      viewBox="0 0 256 256"
                      xmlns="http://www.w3.org/2000/svg"
                    >
                      <path d="M228,128A100,100,0,1,0,60.71,201.90967a3.97048,3.97048,0,0,0,.842.751,99.79378,99.79378,0,0,0,132.8982-.00195,3.96558,3.96558,0,0,0,.83813-.74756A99.76267,99.76267,0,0,0,228,128ZM36,128a92,92,0,1,1,157.17139,64.87207,75.616,75.616,0,0,0-44.50782-34.04053,44,44,0,1,0-41.32714,0,75.61784,75.61784,0,0,0-44.50782,34.04A91.70755,91.70755,0,0,1,36,128Zm92,28a36,36,0,1,1,36-36A36.04061,36.04061,0,0,1,128,156ZM68.86475,198.417a68.01092,68.01092,0,0,1,118.27.00049,91.80393,91.80393,0,0,1-118.27-.00049Z" />
                    </svg>
                  </button>
                  <ul className="absolute right-0 m-0 hidden w-60 -translate-y-2 list-none rounded-tl-md rounded-b-md bg-lime-300 text-indigo-500 group-hover:block">
                    <li className="p-2 text-sm">
                      Signed in as:
                      <p className="w-full overflow-hidden font-bold text-nowrap overflow-ellipsis">
                        {emailAddress}
                      </p>
                    </li>
                    <li className="border-t border-lime-400 p-2 text-sm hover:bg-lime-400">
                      <Link to="/profile">Profile</Link>
                    </li>
                    <li className="border-t border-lime-400 p-2 text-sm hover:bg-lime-400">
                      <Link to="/changepassword">Change Password</Link>
                    </li>
                    <li className="mb-2 border-t border-b border-lime-400 p-2 text-sm hover:bg-lime-400">
                      <a href="#" onClick={handleSignOut}>
                        Sign Out
                      </a>
                    </li>
                  </ul>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </header>
  )
}
