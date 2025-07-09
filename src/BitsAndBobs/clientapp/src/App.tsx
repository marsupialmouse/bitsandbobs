import { Header } from './components/Header/Header.tsx'
import { BrowserRouter, Route, Routes } from 'react-router'
import Home from './features/home/Home.tsx'
import { useGetUserContextQuery } from './features/usercontext/userContextApiSlice.ts'
import SignUp from './features/identity/SignUp/SignUp.tsx'
import RecentEmailsByAddress from './features/email/RecentEmailsByAddress/RecentEmailsByAddress.tsx'
import Loading from './components/Loading.tsx'
import ErrorMessage from './components/ErrorMessage.tsx'
import RecentEmailsByUser from './features/email/RecentEmailsByUser/RecentEmailsByUser.tsx'

function App() {
  const { isLoading, isError } = useGetUserContextQuery()

  if (isLoading) {
    return <Loading />
  }

  if (isError) {
    return (
      <ErrorMessage message="An error occurred while loading the application. Please try again later." />
    )
  }

  return (
    <BrowserRouter>
      <Header />
      <main className="mx-auto mt-16 max-w-7xl px-4 sm:px-6 lg:px-8">
        <Routes>
          <Route index element={<Home />} />
          <Route path="/signup" element={<SignUp />} />
          <Route
            path="/email/recent/a/:emailAddress"
            element={<RecentEmailsByAddress />}
          />
          <Route path="/email/recent/u" element={<RecentEmailsByUser />} />
          {/*
        <Route path="/add-lot" element={<AddLot />} />

        <Route path="/signin" element={<SignIn />} />
        <Route path="/profile" element={<Profile />} />
        <Route path="/l/:id" element={<LotDetails />} />
        <Route path="*" element={<NotFound />} />
        */}
        </Routes>
      </main>
    </BrowserRouter>
  )
}

export default App
