import { screen, waitFor } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import { describe, expect, it } from 'vitest'
import GetMcpTokenButton from './GetMcpTokenButton.tsx'
import { renderWithProviders, userEvent } from '../../../testing/test-utils'

const server = setupServer()

describe('GetMCPTokenButton Component', () => {
  let clipboardWriteTextMock: ReturnType<typeof vi.fn>

  beforeAll(() => {
    server.listen()
  })

  beforeEach(() => {
    vi.useFakeTimers({ shouldAdvanceTime: true })

    // Mock clipboard API
    clipboardWriteTextMock = vi.fn().mockResolvedValue(undefined)
    Object.defineProperty(navigator, 'clipboard', {
      value: {
        writeText: clipboardWriteTextMock,
      },
      writable: true,
    })
  })

  afterEach(() => {
    vi.restoreAllMocks()
    server.resetHandlers()
  })

  afterAll(() => {
    server.close()
  })

  it('renders the Get MCP Token button', () => {
    renderWithProviders(<GetMcpTokenButton />)

    expect(
      screen.getByRole('button', { name: 'Get MCP Token' })
    ).toBeInTheDocument()
  })

  it('calls getJwtToken API when button is clicked', async () => {
    let jwtTokenCalled = false
    server.use(
      http.post('/api/identity/jwt', () => {
        jwtTokenCalled = true
        return HttpResponse.json({ token: 'mock-jwt-token-123' })
      })
    )

    renderWithProviders(<GetMcpTokenButton />)

    await userEvent.click(screen.getByRole('button', { name: 'Get MCP Token' }))

    expect(jwtTokenCalled).toBe(true)
  })

  it('copies token to clipboard when API call succeeds', async () => {
    server.use(
      http.post('/api/identity/jwt', () => {
        return HttpResponse.json({ token: 'mock-jwt-token-123' })
      })
    )

    renderWithProviders(<GetMcpTokenButton />)

    await userEvent.click(screen.getByRole('button', { name: 'Get MCP Token' }))

    await waitFor(() => {
      expect(clipboardWriteTextMock).toHaveBeenCalledWith('mock-jwt-token-123')
    })
  })

  it('changes button text to success message after token is copied', async () => {
    server.use(
      http.post('/api/identity/jwt', () => {
        return HttpResponse.json({ token: 'mock-jwt-token-123' })
      })
    )

    renderWithProviders(<GetMcpTokenButton />)

    await userEvent.click(screen.getByRole('button', { name: 'Get MCP Token' }))

    await waitFor(() => {
      expect(
        screen.getByRole('button', { name: '✓ Token copied to clipboard' })
      ).toBeInTheDocument()
    })
  })

  it('shows loading state while getting token', async () => {
    server.use(
      http.post('/api/identity/jwt', async () => {
        await new Promise((resolve) => setTimeout(resolve, 100))
        return HttpResponse.json({ token: 'mock-jwt-token-123' })
      })
    )

    renderWithProviders(<GetMcpTokenButton />)

    const mcpButton = screen.getByRole('button', { name: 'Get MCP Token' })
    await userEvent.click(mcpButton)

    expect(screen.getByText('Getting token...')).toBeInTheDocument()
    expect(mcpButton).toBeDisabled()
  })

  it('resets button state after 5 seconds', async () => {
    server.use(
      http.post('/api/identity/jwt', () => {
        return HttpResponse.json({ token: 'mock-jwt-token-123' })
      })
    )

    renderWithProviders(<GetMcpTokenButton />)

    const mcpButton = screen.getByRole('button', { name: 'Get MCP Token' })
    await userEvent.click(mcpButton)

    await waitFor(() => {
      expect(
        screen.getByRole('button', { name: '✓ Token copied to clipboard' })
      ).toBeInTheDocument()
    })

    // Fast-forward time by 5 seconds
    vi.advanceTimersByTime(5000)

    await waitFor(() => {
      expect(
        screen.getByRole('button', { name: 'Get MCP Token' })
      ).toBeInTheDocument()
    })
  })

  it('handles missing token in response', async () => {
    server.use(
      http.post('/api/identity/jwt', () => {
        return HttpResponse.json({}) // No token property
      })
    )

    renderWithProviders(<GetMcpTokenButton />)

    const mcpButton = screen.getByRole('button', { name: 'Get MCP Token' })
    await userEvent.click(mcpButton)

    // Button should remain in normal state since no token was copied
    expect(
      await screen.findByRole('button', { name: 'Get MCP Token' })
    ).toBeInTheDocument()
    expect(clipboardWriteTextMock).not.toHaveBeenCalled()
  })

  it('is disabled during API call', async () => {
    server.use(
      http.post('/api/identity/jwt', async () => {
        await new Promise((resolve) => setTimeout(resolve, 100))
        return HttpResponse.json({ token: 'mock-jwt-token-123' })
      })
    )

    renderWithProviders(<GetMcpTokenButton />)

    const button = screen.getByRole('button', { name: 'Get MCP Token' })
    await userEvent.click(button)

    expect(button).toBeDisabled()
  })
})
