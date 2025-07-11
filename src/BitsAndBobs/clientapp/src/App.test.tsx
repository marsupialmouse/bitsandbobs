import { describe, expect, it } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import App from './App'
import { renderWithProviders } from './testing/test-utils'
import { setupServer } from 'msw/node'
import { http, HttpResponse } from 'msw'

const server = setupServer()

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

    renderWithProviders(<App />)

    const spinner = screen.getByRole('status')
    expect(spinner).toBeInTheDocument()
    expect(spinner).toHaveClass('animate-spin')
  })

  it('renders header and home route after loading', async () => {
    server.use(
      http.get('/api/usercontext', () =>
        HttpResponse.json({ isAuthenticated: false })
      )
    )
    renderWithProviders(<App />)

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

    renderWithProviders(<App />)

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

  it('is sets context as authenticated when response is authenticated', async () => {
    server.use(
      http.get('/api/usercontext', () =>
        HttpResponse.json({
          isAuthenticated: true,
          emailAddress: 'ted@interested.com',
        })
      )
    )
    const { store } = renderWithProviders(<App />)

    await waitFor(() => {
      expect(store.getState().userContext.isAuthenticated).toBe(true)
      expect(store.getState().userContext.emailAddress).toBe(
        'ted@interested.com'
      )
    })
  })

  it('is sets context as unauthenticated when response is unauthenticated', async () => {
    server.use(
      http.get('/api/usercontext', () =>
        HttpResponse.json({
          isAuthenticated: false,
        })
      )
    )
    const { store } = renderWithProviders(<App />)

    await waitFor(() => {
      expect(store.getState().userContext.isAuthenticated).toBe(false)
      expect(store.getState().userContext.emailAddress).not.toBeDefined()
    })
  })
})
