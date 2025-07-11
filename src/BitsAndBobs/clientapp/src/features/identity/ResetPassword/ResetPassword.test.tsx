import { screen, waitFor } from '@testing-library/react'
import { HttpResponse, http } from 'msw'
import { setupServer } from 'msw/node'
import { describe, expect, it } from 'vitest'
import ResetPassword from './ResetPassword'
import {
  renderWithProvidersAndRouter,
  userEvent,
} from '../../../testing/test-utils'

const server = setupServer()

describe('Reset Password Component', () => {
  beforeAll(() => {
    server.listen()
  })
  afterEach(() => {
    server.resetHandlers()
  })
  afterAll(() => {
    server.close()
  })

  it('renders the reset password form', () => {
    renderWithProvidersAndRouter(<ResetPassword />, {
      initialEntries: ['/resetpassword?email=t@t.com&code=2'],
    })

    expect(
      screen.getByRole('heading', { name: 'Reset Password' })
    ).toBeInTheDocument()
    expect(screen.getByLabelText('New password')).toBeInTheDocument()
    expect(screen.getByLabelText('Confirm Password')).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: 'Reset password' })
    ).toBeInTheDocument()
  })

  it('validates required fields', async () => {
    renderWithProvidersAndRouter(<ResetPassword />, {
      initialEntries: ['/resetpassword?email=t@t.com&code=2'],
    })

    await userEvent.click(
      screen.getByRole('button', { name: 'Reset password' })
    )

    expect(screen.getByLabelText('New password')).toBeInvalid()
    expect(screen.getByLabelText('Confirm Password')).toBeInvalid()
  })

  it('validates password requirements', async () => {
    renderWithProvidersAndRouter(<ResetPassword />, {
      initialEntries: ['/resetpassword?email=t@t.com&code=2'],
    })

    await userEvent.type(screen.getByLabelText('New password'), 'weak')
    await userEvent.type(screen.getByLabelText('Confirm Password'), 'weak')
    await userEvent.click(
      screen.getByRole('button', { name: 'Reset password' })
    )

    expect(screen.getByLabelText('New password')).toBeInvalid()
  })

  it('validates password confirmation match', async () => {
    renderWithProvidersAndRouter(<ResetPassword />, {
      initialEntries: ['/resetpassword?email=t@t.com&code=2'],
    })

    await userEvent.type(screen.getByLabelText('New password'), 'StrongP@ss123')
    await userEvent.type(
      screen.getByLabelText('Confirm Password'),
      'DifferentP@ss123'
    )
    await userEvent.click(
      screen.getByRole('button', { name: 'Reset password' })
    )

    expect(screen.getByLabelText('Confirm Password')).toBeInvalid()
  })

  it('handles successful password reset', async () => {
    server.use(
      http.post('/api/identity/resetPassword', () => {
        return new HttpResponse(null, { status: 200 })
      })
    )

    renderWithProvidersAndRouter(<ResetPassword />, {
      initialEntries: ['/resetpassword?email=t@t.com&code=2'],
    })

    await userEvent.type(screen.getByLabelText('New password'), 'StrongP@ss123')
    await userEvent.type(
      screen.getByLabelText('Confirm Password'),
      'StrongP@ss123'
    )
    await userEvent.click(
      screen.getByRole('button', { name: 'Reset password' })
    )

    await waitFor(() => {
      expect(screen.getByText('It was a great success!')).toBeInTheDocument()
      const signInLink = screen.getByRole('link', { name: 'sign in' })
      expect(signInLink).toHaveAttribute('href', '/login')
    })
  })

  it('shows loading state while submitting', async () => {
    server.use(
      http.post('/api/identity/resetPassword', async () => {
        await new Promise((resolve) => setTimeout(resolve, 100))
        return new HttpResponse(null, { status: 200 })
      })
    )

    renderWithProvidersAndRouter(<ResetPassword />, {
      initialEntries: ['/resetpassword?email=t@t.com&code=2'],
    })

    await userEvent.type(screen.getByLabelText('New password'), 'StrongP@ss123')
    await userEvent.type(
      screen.getByLabelText('Confirm Password'),
      'StrongP@ss123'
    )

    const submitButton = screen.getByRole('button', { name: 'Reset password' })
    await userEvent.click(submitButton)

    expect(screen.getByText('Resetting password...')).toBeInTheDocument()
    expect(submitButton).toBeDisabled()
  })

  it('shows error when email not in query', () => {
    renderWithProvidersAndRouter(<ResetPassword />, {
      initialEntries: ['/resetpassword?email=&code=2'],
    })

    expect(
      screen.getByText(
        'Unable to reset password: no email address provided. Please check your link.'
      )
    ).toBeInTheDocument()
    expect(
      screen.queryByRole('heading', { name: 'Reset Password' })
    ).not.toBeInTheDocument()
  })

  it('shows error when reset code not in query', () => {
    renderWithProvidersAndRouter(<ResetPassword />, {
      initialEntries: ['/resetpassword?email=t@t.com&code='],
    })

    expect(
      screen.getByText(
        'Unable to reset password: no reset code provided. Please check your link.'
      )
    ).toBeInTheDocument()
    expect(
      screen.queryByRole('heading', { name: 'Reset Password' })
    ).not.toBeInTheDocument()
  })

  it('shows invalid token error message', async () => {
    server.use(
      http.post('/api/identity/resetPassword', () => {
        return HttpResponse.json(
          { errors: { InvalidToken: ['Invalid token'] } },
          { status: 400 }
        )
      })
    )

    renderWithProvidersAndRouter(<ResetPassword />, {
      initialEntries: ['/resetpassword?email=t@t.com&code=2'],
    })

    await userEvent.type(screen.getByLabelText('New password'), 'StrongP@ss123')
    await userEvent.type(
      screen.getByLabelText('Confirm Password'),
      'StrongP@ss123'
    )
    await userEvent.click(
      screen.getByRole('button', { name: 'Reset password' })
    )

    await waitFor(() => {
      expect(
        screen.getByText(
          'The reset code is invalid. The link you clicked may have been corrupted.'
        )
      ).toBeInTheDocument()
    })
  })

  it('shows generic error message for unexpected errors', async () => {
    server.use(
      http.post('/api/identity/resetPassword', () => {
        return new HttpResponse(null, { status: 500 })
      })
    )

    renderWithProvidersAndRouter(<ResetPassword />, {
      initialEntries: ['/resetpassword?email=t@t.com&code=2'],
    })

    await userEvent.type(screen.getByLabelText('New password'), 'StrongP@ss123')
    await userEvent.type(
      screen.getByLabelText('Confirm Password'),
      'StrongP@ss123'
    )
    await userEvent.click(
      screen.getByRole('button', { name: 'Reset password' })
    )

    await waitFor(() => {
      expect(
        screen.getByText(
          'An unexpected error occurred resetting your password, please try again later'
        )
      ).toBeInTheDocument()
    })
  })
})
