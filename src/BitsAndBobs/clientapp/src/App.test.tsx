import { describe, expect, it } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import App from './App'
import { renderWithProvider } from './testing/test-utils'
import { setupServer } from 'msw/node'
import { http, HttpResponse } from 'msw'

const server = setupServer(
  http.get('/api/usercontext', () => {
    return HttpResponse.json({ isAuthenticated: false })
  })
)

describe('App', () => {
  beforeAll(() => {
    server.listen()
  })
  afterEach(() => {
    server.resetHandlers()
  })
  afterAll(() => {
    server.close()
  })

  it('shows loading spinner initially', () => {
    server.use(
      http.get('/api/usercontext', async () => {
        await new Promise((resolve) => setTimeout(resolve, 600))
        return HttpResponse.json({ isAuthenticated: false })
      })
    )

    renderWithProvider(<App />)

    const spinner = screen.getByRole('status')
    expect(spinner).toBeInTheDocument()
    expect(spinner).toHaveClass('animate-spin')
  })

  it('renders header and home route after loading', async () => {
    renderWithProvider(<App />)

    await waitFor(() => {
      expect(screen.getByRole('main')).toBeInTheDocument() // Home
      expect(screen.getByRole('banner')).toBeInTheDocument() // Header
    })
  })

  it('handles api error gracefully', async () => {
    server.use(
      http.get('/api/usercontext', () => {
        return new HttpResponse(null, { status: 500 })
      })
    )

    renderWithProvider(<App />)

    await waitFor(() => {
      expect(
        screen.getByText(
          'An error occurred while loading the application. Please try again later.'
        )
      ).toBeInTheDocument()
      expect(screen.queryByRole('banner')).not.toBeInTheDocument()
      expect(screen.queryByRole('main')).not.toBeInTheDocument()
    })
  })
})
