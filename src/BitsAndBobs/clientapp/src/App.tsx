import { Header } from './components/Header/Header.tsx'
import { BrowserRouter, Route, Routes } from 'react-router'
import Home from './features/home/Home.tsx'
import { useGetUserContextQuery } from './features/usercontext/userContextApiSlice.ts'

function App() {
  const { isLoading, isError } = useGetUserContextQuery()

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div
          className="h-6 w-6 animate-spin rounded-full border-t-2 border-indigo-700"
          role="status"
        />
      </div>
    )
  }

  if (isError) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="text-red-500">
          An error occurred while loading the application. Please try again
          later.
        </div>
      </div>
    )
  }

  return (
    <BrowserRouter>
      <Header />
      <Routes>
        <Route index element={<Home />} />
        {/*
        <Route path="/add-lot" element={<AddLot />} />
        <Route path="/signup" element={<SignUp />} />
        <Route path="/signin" element={<SignIn />} />
        <Route path="/profile" element={<Profile />} />
        <Route path="/l/:id" element={<LotDetails />} />
        <Route path="*" element={<NotFound />} />
        */}
      </Routes>
    </BrowserRouter>
  )
}

export default App
