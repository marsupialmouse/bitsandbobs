import { screen, waitFor } from '@testing-library/react'
import { HttpResponse, http } from 'msw'
import { setupServer } from 'msw/node'
import { describe, expect, it } from 'vitest'
import Login from './Login'
import {
  renderWithProvidersAndRouter,
  userEvent,
} from '../../../testing/test-utils'

const server = setupServer()

describe('Login Component', () => {
  beforeAll(() => {
    server.listen()
  })
  afterEach(() => {
    server.resetHandlers()
  })
  afterAll(() => {
    server.close()
  })

  it('renders the login form', () => {
    renderWithProvidersAndRouter(<Login />)

    expect(screen.getByRole('heading', { name: 'Sign In' })).toBeInTheDocument()
    expect(screen.getByLabelText('Email')).toBeInTheDocument()
    expect(screen.getByLabelText('Password')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Sign In' })).toBeInTheDocument()
  })

  it('validates required fields', async () => {
    renderWithProvidersAndRouter(<Login />)

    await userEvent.click(screen.getByRole('button', { name: 'Sign In' }))

    expect(screen.getByLabelText('Email')).toBeInvalid()
    expect(screen.getByLabelText('Password')).toBeInvalid()
  })

  it('handles successful login', async () => {
    server.use(
      http.post('/api/identity/login', () => {
        return new HttpResponse(null, { status: 200 })
      })
    )

    const { store, getCurrentLocation } = renderWithProvidersAndRouter(
      <Login />,
      { initialEntries: ['/login'] }
    )

    await userEvent.type(screen.getByLabelText('Email'), 'test@example.com')
    await userEvent.type(screen.getByLabelText('Password'), 'password123')
    await userEvent.click(screen.getByRole('button', { name: 'Sign In' }))

    await waitFor(() => {
      const location = getCurrentLocation()
      expect(location?.pathname).toBe('/')
      expect(location?.search ?? '').toBe('')
    })
    const state = store.getState()
    expect(state.userContext.isAuthenticated).toBe(true)
    expect(state.userContext.emailAddress).toBe('test@example.com')
  })

  it('handles redirects to previous page when from in location state', async () => {
    server.use(
      http.post('/api/identity/login', () => {
        return new HttpResponse(null, { status: 200 })
      })
    )

    const { getCurrentLocation } = renderWithProvidersAndRouter(<Login />, {
      initialEntries: [
        { pathname: '/login', state: { from: '/previous-page?hello' } },
      ],
    })

    await userEvent.type(screen.getByLabelText('Email'), 'test@example.com')
    await userEvent.type(screen.getByLabelText('Password'), 'password123')
    await userEvent.click(screen.getByRole('button', { name: 'Sign In' }))

    await waitFor(() => {
      const location = getCurrentLocation()
      expect(location?.pathname).toBe('/previous-page')
      expect(location?.search).toBe('?hello')
    })
  })

  it('shows loading state while submitting', async () => {
    server.use(
      http.post('/api/identity/login', async () => {
        await new Promise((resolve) => setTimeout(resolve, 100))
        return new HttpResponse(null, { status: 200 })
      })
    )

    renderWithProvidersAndRouter(<Login />)

    await userEvent.type(screen.getByLabelText('Email'), 'test@example.com')
    await userEvent.type(screen.getByLabelText('Password'), 'password123')

    const submitButton = screen.getByRole('button', { name: 'Sign In' })
    await userEvent.click(submitButton)

    expect(screen.getByText('Signing in...')).toBeInTheDocument()
    expect(submitButton).toBeDisabled()
  })

  it('shows invalid credentials message', async () => {
    server.use(
      http.post('/api/identity/login', () => {
        return HttpResponse.json(
          { status: 401, detail: 'BadMan' },
          { status: 401 }
        )
      })
    )

    renderWithProvidersAndRouter(<Login />)

    await userEvent.type(screen.getByLabelText('Email'), 'test@example.com')
    await userEvent.type(screen.getByLabelText('Password'), 'wrongpassword')
    await userEvent.click(screen.getByRole('button', { name: 'Sign In' }))

    await waitFor(() => {
      expect(screen.getByText('Invalid email or password')).toBeInTheDocument()
    })
  })

  it('shows account locked message', async () => {
    server.use(
      http.post('/api/identity/login', () => {
        return HttpResponse.json(
          { detail: 'LockedOut', status: 401 },
          { status: 401 }
        )
      })
    )

    renderWithProvidersAndRouter(<Login />)

    await userEvent.type(screen.getByLabelText('Email'), 'test@example.com')
    await userEvent.type(screen.getByLabelText('Password'), 'password123')
    await userEvent.click(screen.getByRole('button', { name: 'Sign In' }))

    await waitFor(() => {
      expect(
        screen.getByText('Your account is locked, please try again later')
      ).toBeInTheDocument()
    })
  })

  it('shows generic error message for unexpected errors', async () => {
    server.use(
      http.post('/api/identity/login', () => {
        return new HttpResponse(null, { status: 500 })
      })
    )

    renderWithProvidersAndRouter(<Login />)

    await userEvent.type(screen.getByLabelText('Email'), 'test@example.com')
    await userEvent.type(screen.getByLabelText('Password'), 'password123')
    await userEvent.click(screen.getByRole('button', { name: 'Sign In' }))

    await waitFor(() => {
      expect(
        screen.getByText(
          'An unexpected error occurred during login, please try again later'
        )
      ).toBeInTheDocument()
    })
  })

  it('navigates home when already signed in', async () => {
    const { getCurrentLocation } = renderWithProvidersAndRouter(<Login />, {
      preloadedState: {
        userContext: { isAuthenticated: true, localTimeOffset: 0 },
      },
      initialEntries: [{ pathname: '/login' }],
    })

    await waitFor(() => {
      const location = getCurrentLocation()
      expect(location?.pathname).toBe('/')
      expect(location?.search).toBe('')
    })
    expect(
      screen.queryByRole('heading', { name: 'Sign In' })
    ).not.toBeInTheDocument()
  })
})
