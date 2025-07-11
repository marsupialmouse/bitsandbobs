import { screen } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router'
import { describe, it, expect } from 'vitest'
import { setupServer } from 'msw/node'
import { http, HttpResponse } from 'msw'
import RecentEmailsByAddress from './RecentEmailsByAddress'
import { renderWithProviders } from '../../../testing/test-utils.tsx'

const mockEmails = [
  {
    recipient: 'test@example.com',
    type: 'Welcome Email',
    body: 'Test body',
    sentAt: '2025-07-09T10:00:00Z',
  },
]

const server = setupServer(
  http.get('/api/emails/recent/:emailAddress', () => {
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

const renderRecentEmailsByAddress = (emailAddress?: string) => {
  const path = emailAddress
    ? `/email/recent/a/${emailAddress}`
    : '/email/recent/a'

  return renderWithProviders(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route
          path="/email/recent/a/:emailAddress?"
          element={<RecentEmailsByAddress />}
        />
      </Routes>
    </MemoryRouter>
  )
}

describe('RecentEmailsByAddress', () => {
  it('displays error when email address is missing', () => {
    renderRecentEmailsByAddress()
    expect(
      screen.getByText('Email address is required to view recent emails.')
    ).toBeInTheDocument()
  })

  it('displays recent emails for valid email address', async () => {
    renderRecentEmailsByAddress('hello@example.com')

    // Wait for the heading to appear
    expect(
      await screen.findByText('Recent emails for hello@example.com')
    ).toBeInTheDocument()
    expect(screen.getByText('Welcome Email')).toBeInTheDocument()
    expect(screen.getByText('Sent to: test@example.com')).toBeInTheDocument()
  })
})
