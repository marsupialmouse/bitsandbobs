import { screen, waitFor } from '@testing-library/react'
import { HttpResponse, http } from 'msw'
import { setupServer } from 'msw/node'
import { describe, expect, it } from 'vitest'
import Profile from './Profile'
import {
  renderWithProvidersAndRouter,
  userEvent,
} from '../../../testing/test-utils'

const server = setupServer()

const mockUserDetails = {
  firstName: 'John',
  lastName: 'Doe',
  displayName: 'johndoe',
  emailAddress: 'john@example.com',
}

describe('Profile Component', () => {
  beforeAll(() => {
    server.listen()
  })
  afterEach(() => {
    server.resetHandlers()
  })
  afterAll(() => {
    server.close()
  })

  it('shows loading state while fetching user details', () => {
    server.use(
      http.get('/api/identity/details', async () => {
        await new Promise((resolve) => setTimeout(resolve, 100))
        return HttpResponse.json(mockUserDetails)
      })
    )

    renderWithProvidersAndRouter(<Profile />)

    expect(screen.getByRole('status')).toBeInTheDocument()
  })

  it('shows error message when unable to load user details', async () => {
    server.use(
      http.get('/api/identity/details', () => {
        return new HttpResponse(null, { status: 500 })
      })
    )

    renderWithProvidersAndRouter(<Profile />)

    await waitFor(() => {
      expect(
        screen.getByText(
          'Unable to load profile details. Please try again later.'
        )
      ).toBeInTheDocument()
    })
  })

  it('renders the profile form with user details', async () => {
    server.use(
      http.get('/api/identity/details', () => {
        return HttpResponse.json(mockUserDetails)
      })
    )

    renderWithProvidersAndRouter(<Profile />)

    await waitFor(() => {
      expect(
        screen.getByRole('heading', { name: 'Profile' })
      ).toBeInTheDocument()
    })

    expect(screen.getByLabelText('First name')).toHaveValue('John')
    expect(screen.getByLabelText('Last name')).toHaveValue('Doe')
    expect(screen.getByLabelText('Display name')).toHaveValue('johndoe')
    expect(screen.getByLabelText('Email address')).toHaveValue(
      'john@example.com'
    )
    expect(screen.getByLabelText('Email address')).toBeDisabled()
    expect(
      screen.getByRole('button', { name: 'Update profile' })
    ).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Change' })).toHaveAttribute(
      'href',
      '/changeemail'
    )
  })

  it('populates form with empty values when user details are empty', async () => {
    const emptyUserDetails = {
      firstName: '',
      lastName: '',
      displayName: 'required_display',
      emailAddress: 'user@example.com',
    }

    server.use(
      http.get('/api/identity/details', () => {
        return HttpResponse.json(emptyUserDetails)
      })
    )

    renderWithProvidersAndRouter(<Profile />)

    await waitFor(() => {
      expect(screen.getByLabelText('First name')).toHaveValue('')
      expect(screen.getByLabelText('Last name')).toHaveValue('')
      expect(screen.getByLabelText('Display name')).toHaveValue(
        'required_display'
      )
    })
  })

  it('validates required display name field', async () => {
    server.use(
      http.get('/api/identity/details', () => {
        return HttpResponse.json(mockUserDetails)
      })
    )

    renderWithProvidersAndRouter(<Profile />)

    await waitFor(() => {
      expect(screen.getByLabelText('Display name')).toBeInTheDocument()
    })

    await userEvent.clear(screen.getByLabelText('Display name'))
    await userEvent.click(
      screen.getByRole('button', { name: 'Update profile' })
    )

    expect(screen.getByLabelText('Display name')).toBeInvalid()
  })

  it('allows optional first and last name fields to be empty', async () => {
    server.use(
      http.get('/api/identity/details', () => {
        return HttpResponse.json(mockUserDetails)
      }),
      http.post('/api/identity/details', () => {
        return new HttpResponse(null, { status: 200 })
      })
    )

    renderWithProvidersAndRouter(<Profile />)

    await waitFor(() => {
      expect(screen.getByLabelText('First name')).toBeInTheDocument()
    })

    await userEvent.clear(screen.getByLabelText('First name'))
    await userEvent.clear(screen.getByLabelText('Last name'))
    await userEvent.click(
      screen.getByRole('button', { name: 'Update profile' })
    )

    await waitFor(() => {
      expect(
        screen.getByText('Your profile has been updated successfully.')
      ).toBeInTheDocument()
    })
  })

  it('handles successful profile update', async () => {
    server.use(
      http.get('/api/identity/details', () => {
        return HttpResponse.json(mockUserDetails)
      }),
      http.post('/api/identity/details', () => {
        return new HttpResponse(null, { status: 200 })
      })
    )

    renderWithProvidersAndRouter(<Profile />)

    await waitFor(() => {
      expect(screen.getByLabelText('First name')).toBeInTheDocument()
    })

    await userEvent.clear(screen.getByLabelText('First name'))
    await userEvent.type(screen.getByLabelText('First name'), 'Jane')
    await userEvent.click(
      screen.getByRole('button', { name: 'Update profile' })
    )

    await waitFor(() => {
      expect(
        screen.getByText('Your profile has been updated successfully.')
      ).toBeInTheDocument()
    })

    // Form should still be visible and functional
    expect(screen.getByRole('heading', { name: 'Profile' })).toBeInTheDocument()
    expect(screen.getByLabelText('First name')).toHaveValue('Jane')
  })

  it('shows loading state while submitting', async () => {
    server.use(
      http.get('/api/identity/details', () => {
        return HttpResponse.json(mockUserDetails)
      }),
      http.post('/api/identity/details', async () => {
        await new Promise((resolve) => setTimeout(resolve, 100))
        return new HttpResponse(null, { status: 200 })
      })
    )

    renderWithProvidersAndRouter(<Profile />)

    await waitFor(() => {
      expect(screen.getByLabelText('Display name')).toBeInTheDocument()
    })

    const submitButton = screen.getByRole('button', { name: 'Update profile' })
    await userEvent.click(submitButton)

    expect(screen.getByText('Updating profile...')).toBeInTheDocument()
    expect(submitButton).toBeDisabled()
  })

  it('shows generic error message for unexpected errors', async () => {
    server.use(
      http.get('/api/identity/details', () => {
        return HttpResponse.json(mockUserDetails)
      }),
      http.post('/api/identity/details', () => {
        return new HttpResponse(null, { status: 500 })
      })
    )

    renderWithProvidersAndRouter(<Profile />)

    await waitFor(() => {
      expect(screen.getByLabelText('Display name')).toBeInTheDocument()
    })

    await userEvent.click(
      screen.getByRole('button', { name: 'Update profile' })
    )

    await waitFor(() => {
      expect(
        screen.getByText(
          'An unexpected error occurred updating your profile, please try again later'
        )
      ).toBeInTheDocument()
    })
  })

  it('clears error messages on successful submission', async () => {
    let requestCount = 0
    server.use(
      http.get('/api/identity/details', () => {
        return HttpResponse.json(mockUserDetails)
      }),
      http.post('/api/identity/details', () => {
        requestCount++
        if (requestCount === 1) {
          return new HttpResponse(null, { status: 500 })
        }
        return new HttpResponse(null, { status: 200 })
      })
    )

    renderWithProvidersAndRouter(<Profile />)

    await waitFor(() => {
      expect(screen.getByLabelText('Display name')).toBeInTheDocument()
    })

    // First submission - should show error
    await userEvent.click(
      screen.getByRole('button', { name: 'Update profile' })
    )

    await waitFor(() => {
      expect(
        screen.getByText(
          'An unexpected error occurred updating your profile, please try again later'
        )
      ).toBeInTheDocument()
    })

    // Second submission - should clear error and show success
    await userEvent.click(
      screen.getByRole('button', { name: 'Update profile' })
    )

    await waitFor(() => {
      expect(
        screen.getByText('Your profile has been updated successfully.')
      ).toBeInTheDocument()
    })

    expect(
      screen.queryByText(
        'An unexpected error occurred updating your profile, please try again later'
      )
    ).not.toBeInTheDocument()
  })
})
