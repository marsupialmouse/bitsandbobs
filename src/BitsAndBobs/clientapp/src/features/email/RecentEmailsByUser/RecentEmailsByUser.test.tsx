import { screen } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import { setupServer } from 'msw/node'
import { http, HttpResponse } from 'msw'
import RecentEmailsByUser from './RecentEmailsByUser'
import { renderWithProviderAndRouter } from '../../../testing/test-utils.tsx'

const mockEmails = [
  {
    recipient: 'test@example.com',
    type: 'Welcome Email',
    body: 'Test body',
    sentAt: '2025-07-09T10:00:00Z',
  },
]

const server = setupServer(
  http.get('/api/emails/recent', () => {
    return HttpResponse.json(mockEmails)
  })
)

beforeAll(() => {
  server.listen()
})
afterEach(() => {
  server.resetHandlers()
})
afterAll(() => {
  server.close()
})

const renderRecentEmailsByUser = (isAuthenticated = false) => {
  return renderWithProviderAndRouter(<RecentEmailsByUser />, {
    preloadedState: {
      userContext: {
        isAuthenticated,
        emailAddress: undefined,
      },
    },
  })
}

describe('RecentEmailsByUser', () => {
  it('displays error when user is not authenticated', () => {
    renderRecentEmailsByUser(false)
    expect(
      screen.getByText('You must be signed in to view your recent emails.')
    ).toBeInTheDocument()
  })

  it('displays recent emails for authenticated user', async () => {
    renderRecentEmailsByUser(true)

    // Wait for the heading to appear
    expect(await screen.findByText('Your recent emails')).toBeInTheDocument()
    expect(screen.getByText('Welcome Email')).toBeInTheDocument()
    expect(screen.getByText('Sent to: test@example.com')).toBeInTheDocument()
  })
})
