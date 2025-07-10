import { Header } from './Header.tsx'
import { screen } from '@testing-library/react'
import {
  renderWithProviderAndRouter,
  userEvent,
} from '../../testing/test-utils.tsx'
import { setupServer } from 'msw/node'
import { http, HttpResponse } from 'msw'
import ProtectedRoute from '../ProtectedRoute.tsx'
import { Route, Routes, useLocation } from 'react-router'

const signedInState = {
  preloadedState: {
    userContext: {
      isAuthenticated: true,
      emailAddress: 'jam@man.com',
    },
  },
}

const server = setupServer(
  http.post('/api/identity/signout', () =>
    HttpResponse.json(null, { status: 200 })
  )
)

const currentLocationTestId = 'current-location'
const CurrentLocation = () => {
  const location = useLocation()

  return (
    <div data-testid={currentLocationTestId}>
      {location.pathname + location.search}
    </div>
  )
}

describe('Header', () => {
  beforeAll(() => {
    server.listen()
  })
  afterEach(() => {
    server.resetHandlers()
  })
  afterAll(() => {
    server.close()
  })

  it('renders the site name', () => {
    renderWithProviderAndRouter(<Header />)

    expect(screen.getByText('BITS&BOBS')).toBeInTheDocument()
  })

  it('renders Sign Up and Sign In links when not authenticated', () => {
    renderWithProviderAndRouter(<Header />)

    expect(screen.getByText('Sign Up')).toBeInTheDocument()
    expect(screen.getByText('Sign In')).toBeInTheDocument()
    expect(screen.queryByText('Profile')).not.toBeInTheDocument()
    expect(screen.queryByText('Sign Out')).not.toBeInTheDocument()
  })

  it('renders Profile and Sign Out links when authenticated', () => {
    renderWithProviderAndRouter(<Header />, signedInState)

    expect(screen.getByText('Profile')).toBeInTheDocument()
    expect(screen.getByText('Sign Out')).toBeInTheDocument()
    expect(screen.queryByText('Sign Up')).not.toBeInTheDocument()
    expect(screen.queryByText('Sign In')).not.toBeInTheDocument()
  })

  it('signs user out when Sign Out clicked', async () => {
    const { store } = renderWithProviderAndRouter(<Header />, signedInState)

    await userEvent.click(screen.getByText('Sign Out'))

    expect(await screen.findByText('Sign In')).toBeInTheDocument()
    expect(screen.queryByText('Sign Out')).not.toBeInTheDocument()
    expect(store.getState().userContext.isAuthenticated).toBe(false)
    expect(store.getState().userContext.emailAddress).not.toBeDefined()
  })

  it('navigates home when signing out on protected route', async () => {
    renderWithProviderAndRouter(
      <>
        <Header />
        <Routes>
          <Route element={<ProtectedRoute />}>
            <Route path="/secret" element={<></>} />
          </Route>
          <Route path="*" element={<CurrentLocation />} />
        </Routes>
      </>,
      { ...signedInState, initialEntries: ['/secret'] }
    )

    await userEvent.click(screen.getByText('Sign Out'))

    const location = await screen.findByTestId(currentLocationTestId)
    expect(location.textContent).toBe('/')
  })
})
