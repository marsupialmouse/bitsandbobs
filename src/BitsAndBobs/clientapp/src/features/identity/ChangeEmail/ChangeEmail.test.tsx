import { screen } from '@testing-library/react'
import { HttpResponse, http } from 'msw'
import { setupServer } from 'msw/node'
import { describe, expect, it } from 'vitest'
import ChangeEmail from './ChangeEmail'
import {
  renderWithProvidersAndRouter,
  userEvent,
} from '../../../testing/test-utils'

const server = setupServer()

const mockInitialState = {
  userContext: {
    emailAddress: 'current@example.com',
    isAuthenticated: true,
    localTimeOffset: 0,
  },
}

describe('Change Email Component', () => {
  beforeAll(() => {
    server.listen()
  })
  afterEach(() => {
    server.resetHandlers()
  })
  afterAll(() => {
    server.close()
  })

  it('renders the change email form', () => {
    renderWithProvidersAndRouter(<ChangeEmail />, {
      preloadedState: mockInitialState,
    })

    expect(
      screen.getByRole('heading', { name: 'Change Email Address' })
    ).toBeInTheDocument()
    expect(screen.getByText('Current email address')).toBeInTheDocument()
    expect(screen.getByText('current@example.com')).toBeInTheDocument()
    expect(screen.getByLabelText('New email address')).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: 'Change email address' })
    ).toBeInTheDocument()
  })

  it('validates required fields', async () => {
    renderWithProvidersAndRouter(<ChangeEmail />, {
      preloadedState: mockInitialState,
    })

    await userEvent.click(
      screen.getByRole('button', { name: 'Change email address' })
    )

    expect(screen.getByLabelText('New email address')).toBeInvalid()
  })

  it('validates email format', async () => {
    renderWithProvidersAndRouter(<ChangeEmail />, {
      preloadedState: mockInitialState,
    })

    await userEvent.type(
      screen.getByLabelText('New email address'),
      'invalid-email'
    )
    await userEvent.click(
      screen.getByRole('button', { name: 'Change email address' })
    )

    expect(screen.getByLabelText('New email address')).toBeInvalid()
  })

  it('validates that email address has changed', async () => {
    renderWithProvidersAndRouter(<ChangeEmail />, {
      preloadedState: mockInitialState,
    })

    await userEvent.type(
      screen.getByLabelText('New email address'),
      'current@example.com'
    )
    await userEvent.click(
      screen.getByRole('button', { name: 'Change email address' })
    )

    expect(screen.getByLabelText('New email address')).toBeInvalid()
  })

  it('allows case changes in email address', async () => {
    server.use(
      http.post('/api/identity/manage/info', () => {
        return new HttpResponse(null, { status: 200 })
      })
    )

    renderWithProvidersAndRouter(<ChangeEmail />, {
      preloadedState: mockInitialState,
    })

    await userEvent.type(
      screen.getByLabelText('New email address'),
      'Current@Example.com'
    )
    await userEvent.click(
      screen.getByRole('button', { name: 'Change email address' })
    )

    expect(
      await screen.findByText('It was a great success!', { exact: false })
    ).toBeInTheDocument()
  })

  it('handles successful email change', async () => {
    server.use(
      http.post('/api/identity/manage/info', () => {
        return new HttpResponse(null, { status: 200 })
      })
    )

    renderWithProvidersAndRouter(<ChangeEmail />, {
      preloadedState: mockInitialState,
    })

    await userEvent.type(
      screen.getByLabelText('New email address'),
      'new@example.com'
    )
    await userEvent.click(
      screen.getByRole('button', { name: 'Change email address' })
    )

    expect(
      await screen.findByText('It was a great success!', { exact: false })
    ).toBeInTheDocument()
    const emailPageLink = screen.getByRole('link', {
      name: 'Check your email',
    })
    expect(emailPageLink).toHaveAttribute('href', '/email/recent/u')
  })

  it('shows loading state while submitting', async () => {
    server.use(
      http.post('/api/identity/manage/info', async () => {
        await new Promise((resolve) => setTimeout(resolve, 100))
        return new HttpResponse(null, { status: 200 })
      })
    )

    renderWithProvidersAndRouter(<ChangeEmail />, {
      preloadedState: mockInitialState,
    })

    await userEvent.type(
      screen.getByLabelText('New email address'),
      'new@example.com'
    )

    const submitButton = screen.getByRole('button', {
      name: 'Change email address',
    })
    await userEvent.click(submitButton)

    expect(screen.getByText('Changing email address...')).toBeInTheDocument()
    expect(submitButton).toBeDisabled()
  })

  it('shows generic error message for unexpected errors', async () => {
    server.use(
      http.post('/api/identity/manage/info', () => {
        return new HttpResponse(null, { status: 500 })
      })
    )

    renderWithProvidersAndRouter(<ChangeEmail />, {
      preloadedState: mockInitialState,
    })

    await userEvent.type(
      screen.getByLabelText('New email address'),
      'new@example.com'
    )
    await userEvent.click(
      screen.getByRole('button', { name: 'Change email address' })
    )

    expect(
      await screen.findByText(
        'An unexpected error occurred changing your email address, please try again later'
      )
    ).toBeInTheDocument()
  })
})
