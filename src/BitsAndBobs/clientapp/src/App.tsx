import { Header } from './components/Header/Header.tsx'
import { BrowserRouter, Route, Routes } from 'react-router'
import { useGetUserContextQuery } from './features/usercontext/userContextApiSlice.ts'
import Register from './features/identity/Register/Register.tsx'
import RecentEmailsByAddress from './features/email/RecentEmailsByAddress/RecentEmailsByAddress.tsx'
import Loading from './components/Loading.tsx'
import RecentEmailsByUser from './features/email/RecentEmailsByUser/RecentEmailsByUser.tsx'
import ConfirmEmail from './features/identity/ConfirmEmail/ConfirmEmail.tsx'
import Login from './features/identity/Login/Login.tsx'
import ProtectedRoute from './components/ProtectedRoute.tsx'
import ForgotPassword from './features/identity/ForgotPassword/ForgotPassword.tsx'
import ResetPassword from './features/identity/ResetPassword/ResetPassword.tsx'
import ChangePassword from './features/identity/ChangePassword/ChangePassword.tsx'
import ChangeEmail from './features/identity/ChangeEmail/ChangeEmail.tsx'
import Profile from './features/identity/Profile/Profile.tsx'
import CreateAuction from './features/auctions/CreateAuction/CreateAuction.tsx'
import CurrentAuctions from './features/auctions/CurrentAuctions/CurrentAuctions.tsx'
import Auction from './features/auctions/Auction/Auction.tsx'
import UserAuctions from './features/auctions/UserAuctions/UserAuctions.tsx'
import RelistAuction from './features/auctions/RelistAuction/RelistAuction.tsx'

function App() {
  const { isLoading, isError } = useGetUserContextQuery()

  if (isLoading) {
    return <Loading />
  }

  if (isError) {
    return (
      <div className="h-screen w-full bg-linear-to-bl from-indigo-400 to-indigo-700">
        <div className="flex h-full items-center justify-center">
          <div>
            <h1 className="block font-logo text-7xl font-bold text-lime-300">
              BITS&BOBS
            </h1>
            <div className="text-center text-lime-300">
              It&apos;s broken, try again later.
            </div>
          </div>
        </div>
      </div>
    )
  }

  return (
    <BrowserRouter>
      <Header />
      <main className="mx-auto mt-16 max-w-7xl px-4 sm:px-6 lg:px-8">
        <Routes>
          <Route index element={<CurrentAuctions />} />
          <Route path="/register" element={<Register />} />
          <Route path="/login" element={<Login />} />
          <Route path="/confirmemail" element={<ConfirmEmail />} />
          <Route path="/forgotpassword" element={<ForgotPassword />} />
          <Route path="/resetpassword" element={<ResetPassword />} />
          <Route
            path="/email/recent/a/:emailAddress"
            element={<RecentEmailsByAddress />}
          />
          <Route path="/auction/:id" element={<Auction />} />
          <Route element={<ProtectedRoute />}>
            <Route path="/profile" element={<Profile />} />
            <Route path="/changeemail" element={<ChangeEmail />} />
            <Route path="/changepassword" element={<ChangePassword />} />
            <Route path="/auctions/create" element={<CreateAuction />} />
            <Route path="/auction/:id/relist" element={<RelistAuction />} />
            <Route path="/auctions/my" element={<UserAuctions />} />
            <Route path="/email/recent/u" element={<RecentEmailsByUser />} />
          </Route>
        </Routes>
      </main>
    </BrowserRouter>
  )
}

export default App
