import { screen, waitFor } from '@testing-library/react'
import { HttpResponse, http } from 'msw'
import { setupServer } from 'msw/node'
import { describe, expect, it } from 'vitest'
import ForgotPassword from './ForgotPassword'
import {
  renderWithProvidersAndRouter,
  userEvent,
} from '../../../testing/test-utils'

const server = setupServer()

describe('Forgot Password Component', () => {
  beforeAll(() => {
    server.listen()
  })
  afterEach(() => {
    server.resetHandlers()
  })
  afterAll(() => {
    server.close()
  })

  it('renders the forgot password form', () => {
    renderWithProvidersAndRouter(<ForgotPassword />)

    expect(
      screen.getByRole('heading', { name: 'Forgot your password?' })
    ).toBeInTheDocument()
    expect(screen.getByLabelText('Email')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Send' })).toBeInTheDocument()
  })

  it('validates required field', async () => {
    renderWithProvidersAndRouter(<ForgotPassword />)

    await userEvent.click(screen.getByRole('button', { name: 'Send' }))

    expect(screen.getByLabelText('Email')).toBeInvalid()
  })

  it('handles successful api call', async () => {
    server.use(
      http.post('/api/identity/forgotPassword', () => {
        return new HttpResponse(null, { status: 200 })
      })
    )

    renderWithProvidersAndRouter(<ForgotPassword />)

    await userEvent.type(screen.getByLabelText('Email'), 'test@example.com')
    await userEvent.click(screen.getByRole('button', { name: 'Send' }))

    await waitFor(() => {
      expect(screen.queryByLabelText('Email')).not.toBeInTheDocument()
      expect(
        screen.queryByRole('button', { name: 'Send' })
      ).not.toBeInTheDocument()
      const emailLink = screen.getByRole('link', { name: /check your email/i })
      expect(emailLink).toHaveAttribute(
        'href',
        '/email/recent/a/test@example.com'
      )
    })
  })

  it('shows loading state while submitting', async () => {
    server.use(
      http.post('/api/identity/forgotPassword', async () => {
        await new Promise((resolve) => setTimeout(resolve, 100))
        return new HttpResponse(null, { status: 200 })
      })
    )

    renderWithProvidersAndRouter(<ForgotPassword />)

    await userEvent.type(screen.getByLabelText('Email'), 'test@example.com')

    const submitButton = screen.getByRole('button', { name: 'Send' })
    await userEvent.click(submitButton)

    expect(screen.getByText('Sending...')).toBeInTheDocument()
    expect(submitButton).toBeDisabled()
  })

  it('shows generic error message for unexpected errors', async () => {
    server.use(
      http.post('/api/identity/forgotPassword', () => {
        return new HttpResponse(null, { status: 500 })
      })
    )

    renderWithProvidersAndRouter(<ForgotPassword />)

    await userEvent.type(screen.getByLabelText('Email'), 'test@example.com')
    await userEvent.click(screen.getByRole('button', { name: 'Send' }))

    await waitFor(() => {
      expect(
        screen.getByText('An unexpected error occurred, please try again later')
      ).toBeInTheDocument()
    })
  })

  it('navigates home when already signed in', async () => {
    const { getCurrentLocation } = renderWithProvidersAndRouter(
      <ForgotPassword />,
      {
        preloadedState: { userContext: { isAuthenticated: true } },
        initialEntries: [{ pathname: '/forgotPassword' }],
      }
    )

    await waitFor(() => {
      const location = getCurrentLocation()
      expect(location?.pathname).toBe('/')
      expect(location?.search).toBe('')
    })
    expect(
      screen.queryByRole('heading', { name: 'Forgot your password?' })
    ).not.toBeInTheDocument()
  })
})
