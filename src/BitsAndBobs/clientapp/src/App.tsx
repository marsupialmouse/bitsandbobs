import { Header } from './components/Header/Header.tsx'
import { Route, Routes } from 'react-router'
import Home from './features/home/Home.tsx'

function App() {
  return (
    <>
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
    </>
  )
}

export default App
