import { screen, waitFor } from '@testing-library/react'
import { HttpResponse, http } from 'msw'
import { setupServer } from 'msw/node'
import { describe, expect, it } from 'vitest'
import Register from './Register.tsx'
import {
  renderWithProviderAndRouter,
  userEvent,
} from '../../../testing/test-utils.tsx'

const server = setupServer()

describe('Register Component', () => {
  beforeAll(() => {
    server.listen()
  })
  afterEach(() => {
    server.resetHandlers()
  })
  afterAll(() => {
    server.close()
  })

  it('renders the signup form correctly', () => {
    renderWithProviderAndRouter(<Register />)

    expect(
      screen.getByRole('heading', { name: /sign up/i })
    ).toBeInTheDocument()
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/^password$/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/confirm password/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /sign up/i })).toBeInTheDocument()
  })

  it('validates required fields', async () => {
    renderWithProviderAndRouter(<Register />)

    const submitButton = screen.getByRole('button', { name: /sign up/i })
    await userEvent.click(submitButton)

    expect(screen.getByLabelText(/email/i)).toBeInvalid()
    expect(screen.getByLabelText(/^password$/i)).toBeInvalid()
    expect(screen.getByLabelText(/confirm password/i)).toBeInvalid()
  })

  it('validates password requirements', async () => {
    renderWithProviderAndRouter(<Register />)

    const passwordInput = screen.getByLabelText(/^password$/i)
    await userEvent.type(passwordInput, 'weak')

    expect(passwordInput).toBeInvalid()
  })

  it('validates password confirmation match', async () => {
    renderWithProviderAndRouter(<Register />)

    const passwordInput = screen.getByLabelText(/^password$/i)
    const confirmPasswordInput = screen.getByLabelText(/confirm password/i)

    await userEvent.type(passwordInput, 'StrongP@ss123')
    await userEvent.type(confirmPasswordInput, 'DifferentP@ss123')

    expect(confirmPasswordInput).toBeInvalid()
  })

  it('submits form with valid data and shows success message', async () => {
    server.use(
      http.post('/api/identity/register', () => {
        return new HttpResponse(null, { status: 200 })
      })
    )

    renderWithProviderAndRouter(<Register />)

    const emailInput = screen.getByLabelText(/email/i)
    const passwordInput = screen.getByLabelText(/^password$/i)
    const confirmPasswordInput = screen.getByLabelText(/confirm password/i)

    await userEvent.type(emailInput, 'test@example.com')
    await userEvent.type(passwordInput, 'StrongP@ss123')
    await userEvent.type(confirmPasswordInput, 'StrongP@ss123')

    const submitButton = screen.getByRole('button', { name: /sign up/i })
    await userEvent.click(submitButton)

    await waitFor(() => {
      expect(screen.getByText(/registration successful/i)).toBeInTheDocument()
      expect(screen.getByText(/check your email/i)).toBeInTheDocument()
    })
  })

  it('shows loading state while submitting', async () => {
    server.use(
      http.post('/api/identity/register', async () => {
        await new Promise((resolve) => setTimeout(resolve, 100))
        return new HttpResponse(null, { status: 200 })
      })
    )

    renderWithProviderAndRouter(<Register />)

    await userEvent.type(screen.getByLabelText(/email/i), 'test@example.com')
    await userEvent.type(screen.getByLabelText(/^password$/i), 'StrongP@ss123')
    await userEvent.type(
      screen.getByLabelText(/confirm password/i),
      'StrongP@ss123'
    )

    const submitButton = screen.getByRole('button', { name: /sign up/i })
    await userEvent.click(submitButton)

    expect(screen.getByText(/signing up\.\.\./i)).toBeInTheDocument()
    expect(submitButton).toBeDisabled()
  })

  it('displays API error message', async () => {
    const errorMessage = 'Invalid email format'
    server.use(
      http.post('/api/identity/register', () => {
        return HttpResponse.json(
          {
            errors: {
              Email: [errorMessage],
            },
          },
          { status: 400 }
        )
      })
    )

    renderWithProviderAndRouter(<Register />)

    await userEvent.type(screen.getByLabelText(/email/i), 'valid@email.com')
    await userEvent.type(screen.getByLabelText(/^password$/i), 'StrongP@ss123')
    await userEvent.type(
      screen.getByLabelText(/confirm password/i),
      'StrongP@ss123'
    )

    const submitButton = screen.getByRole('button', { name: /sign up/i })
    await userEvent.click(submitButton)

    await waitFor(() => {
      expect(screen.getByText(errorMessage)).toBeInTheDocument()
    })
  })

  it('navigates to email confirmation page after successful registration', async () => {
    server.use(
      http.post('/api/identity/register', () => {
        return new HttpResponse(null, { status: 200 })
      })
    )

    renderWithProviderAndRouter(<Register />)

    const email = 'test@example.com'
    await userEvent.type(screen.getByLabelText(/email/i), email)
    await userEvent.type(screen.getByLabelText(/^password$/i), 'StrongP@ss123')
    await userEvent.type(
      screen.getByLabelText(/confirm password/i),
      'StrongP@ss123'
    )

    await userEvent.click(screen.getByRole('button', { name: /sign up/i }))

    await waitFor(() => {
      const emailLink = screen.getByRole('link', { name: /check your email/i })
      expect(emailLink).toHaveAttribute('href', `/email/recent/a/${email}`)
    })
  })
})
