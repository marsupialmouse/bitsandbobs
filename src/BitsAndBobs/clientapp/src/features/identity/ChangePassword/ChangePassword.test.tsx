import { screen, waitFor } from '@testing-library/react'
import { HttpResponse, http } from 'msw'
import { setupServer } from 'msw/node'
import { describe, expect, it } from 'vitest'
import ChangePassword from './ChangePassword'
import {
  renderWithProvidersAndRouter,
  userEvent,
} from '../../../testing/test-utils'

const server = setupServer()

describe('Change Password Component', () => {
  beforeAll(() => {
    server.listen()
  })
  afterEach(() => {
    server.resetHandlers()
  })
  afterAll(() => {
    server.close()
  })

  it('renders the change password form', () => {
    renderWithProvidersAndRouter(<ChangePassword />)

    expect(
      screen.getByRole('heading', { name: 'Change Password' })
    ).toBeInTheDocument()
    expect(screen.getByLabelText('Current password')).toBeInTheDocument()
    expect(screen.getByLabelText('New password')).toBeInTheDocument()
    expect(screen.getByLabelText('Confirm new password')).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: 'Change password' })
    ).toBeInTheDocument()
  })

  it('validates required fields', async () => {
    renderWithProvidersAndRouter(<ChangePassword />)

    await userEvent.click(
      screen.getByRole('button', { name: 'Change password' })
    )

    expect(screen.getByLabelText('Current password')).toBeInvalid()
    expect(screen.getByLabelText('New password')).toBeInvalid()
    expect(screen.getByLabelText('Confirm new password')).toBeInvalid()
  })

  it('validates password requirements', async () => {
    renderWithProvidersAndRouter(<ChangePassword />)

    await userEvent.type(screen.getByLabelText('Current password'), 'current')
    await userEvent.type(screen.getByLabelText('New password'), 'weak')
    await userEvent.type(screen.getByLabelText('Confirm new password'), 'weak')
    await userEvent.click(
      screen.getByRole('button', { name: 'Change password' })
    )

    expect(screen.getByLabelText('New password')).toBeInvalid()
  })

  it('validates password confirmation match', async () => {
    renderWithProvidersAndRouter(<ChangePassword />)

    await userEvent.type(
      screen.getByLabelText('Current password'),
      'Current123!'
    )
    await userEvent.type(screen.getByLabelText('New password'), 'StrongP@ss123')
    await userEvent.type(
      screen.getByLabelText('Confirm new password'),
      'DifferentP@ss123'
    )
    await userEvent.click(
      screen.getByRole('button', { name: 'Change password' })
    )

    expect(screen.getByLabelText('Confirm new password')).toBeInvalid()
  })

  it('handles successful password change', async () => {
    server.use(
      http.post('/api/identity/manage/info', () => {
        return new HttpResponse(null, { status: 200 })
      })
    )

    renderWithProvidersAndRouter(<ChangePassword />)

    await userEvent.type(
      screen.getByLabelText('Current password'),
      'Current123!'
    )
    await userEvent.type(screen.getByLabelText('New password'), 'StrongP@ss123')
    await userEvent.type(
      screen.getByLabelText('Confirm new password'),
      'StrongP@ss123'
    )
    await userEvent.click(
      screen.getByRole('button', { name: 'Change password' })
    )

    await waitFor(() => {
      expect(screen.getByText('It was a great success!')).toBeInTheDocument()
    })
  })

  it('shows loading state while submitting', async () => {
    server.use(
      http.post('/api/identity/manage/info', async () => {
        await new Promise((resolve) => setTimeout(resolve, 100))
        return new HttpResponse(null, { status: 200 })
      })
    )

    renderWithProvidersAndRouter(<ChangePassword />)

    await userEvent.type(
      screen.getByLabelText('Current password'),
      'Current123!'
    )
    await userEvent.type(screen.getByLabelText('New password'), 'StrongP@ss123')
    await userEvent.type(
      screen.getByLabelText('Confirm new password'),
      'StrongP@ss123'
    )

    const submitButton = screen.getByRole('button', { name: 'Change password' })
    await userEvent.click(submitButton)

    expect(screen.getByText('Changing password...')).toBeInTheDocument()
    expect(submitButton).toBeDisabled()
  })

  it('shows error when current password is incorrect', async () => {
    server.use(
      http.post('/api/identity/manage/info', () => {
        return HttpResponse.json(
          { errors: { PasswordMismatch: ['Invalid current password'] } },
          { status: 400 }
        )
      })
    )

    renderWithProvidersAndRouter(<ChangePassword />)

    await userEvent.type(
      screen.getByLabelText('Current password'),
      'WrongPass123!'
    )
    await userEvent.type(screen.getByLabelText('New password'), 'StrongP@ss123')
    await userEvent.type(
      screen.getByLabelText('Confirm new password'),
      'StrongP@ss123'
    )
    await userEvent.click(
      screen.getByRole('button', { name: 'Change password' })
    )

    await waitFor(() => {
      expect(
        screen.getByText('The current password is incorrect')
      ).toBeInTheDocument()
    })
  })

  it('shows generic error message for unexpected errors', async () => {
    server.use(
      http.post('/api/identity/manage/info', () => {
        return new HttpResponse(null, { status: 500 })
      })
    )

    renderWithProvidersAndRouter(<ChangePassword />)

    await userEvent.type(
      screen.getByLabelText('Current password'),
      'Current123!'
    )
    await userEvent.type(screen.getByLabelText('New password'), 'StrongP@ss123')
    await userEvent.type(
      screen.getByLabelText('Confirm new password'),
      'StrongP@ss123'
    )
    await userEvent.click(
      screen.getByRole('button', { name: 'Change password' })
    )

    await waitFor(() => {
      expect(
        screen.getByText(
          'An unexpected error occurred changing your password, please try again later'
        )
      ).toBeInTheDocument()
    })
  })
})
