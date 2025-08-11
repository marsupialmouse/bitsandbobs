import { screen, waitFor } from '@testing-library/react'
import { HttpResponse, http } from 'msw'
import { setupServer } from 'msw/node'
import { describe, expect, it, vi } from 'vitest'
import CurrentAuctions from './CurrentAuctions'
import { renderWithProvidersAndRouter } from '../../../testing/test-utils'

const server = setupServer()

const mockAuctionsResponse = {
  auctions: [
    {
      id: 'auction-1',
      name: 'Vintage Camera',
      description:
        "A beautiful vintage camera from the 1960s in excellent condition. This classic piece would make a great addition to any photography enthusiast's collection.",
      imageHref: 'https://example.com/camera.jpg',
      currentPrice: 125.5,
      numberOfBids: 5,
      endDate: new Date(Date.now() + 2 * 24 * 60 * 60 * 1000).toISOString(), // 2 days from now
      sellerDisplayName: 'John Doe',
    },
    {
      id: 'auction-2',
      name: 'Antique Vase',
      description: 'Small antique vase',
      imageHref: 'https://example.com/vase.jpg',
      currentPrice: 45.0,
      numberOfBids: 1,
      endDate: new Date(Date.now() + 3 * 60 * 60 * 1000).toISOString(), // 3 hours from now
      sellerDisplayName: 'Jane Smith',
    },
    {
      id: 'auction-3',
      name: 'Old Book',
      description: 'Rare first edition book',
      imageHref: 'https://example.com/book.jpg',
      currentPrice: 85.25,
      numberOfBids: 0,
      endDate: new Date(Date.now() + 30 * 60 * 1000).toISOString(), // 30 minutes from now
      sellerDisplayName: 'Bob Wilson',
    },
  ],
}

const emptyAuctionsResponse = {
  auctions: [],
}

describe('CurrentAuctions Component', () => {
  beforeAll(() => {
    server.listen()
  })

  afterEach(() => {
    server.resetHandlers()
  })

  afterAll(() => {
    server.close()
  })

  it('shows loading state while fetching auctions', () => {
    server.use(
      http.get('/api/auctions', async () => {
        await new Promise((resolve) => setTimeout(resolve, 100))
        return HttpResponse.json(mockAuctionsResponse)
      })
    )

    renderWithProvidersAndRouter(<CurrentAuctions />)

    expect(screen.getByRole('status')).toBeInTheDocument()
  })

  it('shows error message when unable to load auctions', async () => {
    server.use(
      http.get('/api/auctions', () => {
        return new HttpResponse(null, { status: 500 })
      })
    )

    renderWithProvidersAndRouter(<CurrentAuctions />)

    await waitFor(() => {
      expect(
        screen.getByText('Unable to load auctions. Please try again later.')
      ).toBeInTheDocument()
    })
  })

  it('shows empty state when no auctions exist', async () => {
    server.use(
      http.get('/api/auctions', () => {
        return HttpResponse.json(emptyAuctionsResponse)
      })
    )

    renderWithProvidersAndRouter(<CurrentAuctions />)

    await waitFor(() => {
      expect(
        screen.getByText('There are no active auctions :(')
      ).toBeInTheDocument()
    })
  })

  it('renders list of auctions with all required information', async () => {
    server.use(
      http.get('/api/auctions', () => {
        return HttpResponse.json(mockAuctionsResponse)
      })
    )

    renderWithProvidersAndRouter(<CurrentAuctions />)

    await waitFor(() => {
      expect(
        screen.getByRole('heading', { name: 'Current Auctions' })
      ).toBeInTheDocument()
    })

    // Check first auction
    expect(screen.getByText('Vintage Camera')).toBeInTheDocument()
    expect(screen.getByText(/A beautiful vintage camera/)).toBeInTheDocument()
    expect(screen.getByText('$125.50')).toBeInTheDocument()
    expect(screen.getByText('5 bids')).toBeInTheDocument()
    expect(screen.getByText('John Doe')).toBeInTheDocument()

    // Check second auction
    expect(screen.getByText('Antique Vase')).toBeInTheDocument()
    expect(screen.getByText('$45.00')).toBeInTheDocument()
    expect(screen.getByText('1 bid')).toBeInTheDocument()
    expect(screen.getByText('Jane Smith')).toBeInTheDocument()

    // Check third auction
    expect(screen.getByText('Old Book')).toBeInTheDocument()
    expect(screen.getByText('$85.25')).toBeInTheDocument()
    expect(screen.getByText('0 bids')).toBeInTheDocument()
    expect(screen.getByText('Bob Wilson')).toBeInTheDocument()
  })

  it('renders auction images with correct alt text', async () => {
    server.use(
      http.get('/api/auctions', () => {
        return HttpResponse.json(mockAuctionsResponse)
      })
    )

    renderWithProvidersAndRouter(<CurrentAuctions />)

    await waitFor(() => {
      const cameraImage = screen.getByRole('img', { name: 'Vintage Camera' })
      expect(cameraImage).toBeInTheDocument()
      expect(cameraImage).toHaveAttribute(
        'src',
        'https://example.com/camera.jpg'
      )

      const vaseImage = screen.getByRole('img', { name: 'Antique Vase' })
      expect(vaseImage).toBeInTheDocument()
      expect(vaseImage).toHaveAttribute('src', 'https://example.com/vase.jpg')

      const bookImage = screen.getByRole('img', { name: 'Old Book' })
      expect(bookImage).toBeInTheDocument()
      expect(bookImage).toHaveAttribute('src', 'https://example.com/book.jpg')
    })
  })

  it('creates clickable links to individual auctions', async () => {
    server.use(
      http.get('/api/auctions', () => {
        return HttpResponse.json(mockAuctionsResponse)
      })
    )

    renderWithProvidersAndRouter(<CurrentAuctions />)

    await waitFor(() => {
      const cameraLink = screen.getByRole('link', { name: /Vintage Camera/ })
      expect(cameraLink).toHaveAttribute('href', '/auction/auction-1')

      const vaseLink = screen.getByRole('link', { name: /Antique Vase/ })
      expect(vaseLink).toHaveAttribute('href', '/auction/auction-2')

      const bookLink = screen.getByRole('link', { name: /Old Book/ })
      expect(bookLink).toHaveAttribute('href', '/auction/auction-3')
    })
  })

  it('truncates long descriptions correctly', async () => {
    server.use(
      http.get('/api/auctions', () => {
        return HttpResponse.json(mockAuctionsResponse)
      })
    )

    renderWithProvidersAndRouter(<CurrentAuctions />)

    await waitFor(() => {
      // The vintage camera has a long description that should be truncated
      const description = screen.getByText(/A beautiful vintage camera/)
      expect(description.textContent).toContain('...')
      expect(description.textContent?.length).toBeLessThan(140) // 120 + "..."

      // Short descriptions should not be truncated
      expect(screen.getByText('Small antique vase')).toBeInTheDocument()
      expect(screen.getByText('Rare first edition book')).toBeInTheDocument()
    })
  })

  it('formats time remaining correctly', async () => {
    // Mock current time for consistent testing
    const mockNow = new Date('2024-01-01T12:00:00Z')
    vi.setSystemTime(mockNow)

    const testAuctionsResponse = {
      auctions: [
        {
          id: 'auction-1',
          name: 'Test Auction 1',
          description: 'Test description',
          imageUrl: 'https://example.com/test.jpg',
          currentPrice: 10.0,
          numberOfBids: 0,
          endDate: new Date(
            mockNow.getTime() + 2 * 24 * 60 * 60 * 1000
          ).toISOString(), // 2 days
          sellerDisplayName: 'Test Seller',
        },
        {
          id: 'auction-2',
          name: 'Test Auction 2',
          description: 'Test description',
          imageUrl: 'https://example.com/test.jpg',
          currentPrice: 10.0,
          numberOfBids: 0,
          endDate: new Date(
            mockNow.getTime() + 3 * 60 * 60 * 1000
          ).toISOString(), // 3 hours
          sellerDisplayName: 'Test Seller',
        },
        {
          id: 'auction-3',
          name: 'Test Auction 3',
          description: 'Test description',
          imageUrl: 'https://example.com/test.jpg',
          currentPrice: 10.0,
          numberOfBids: 0,
          endDate: new Date(mockNow.getTime() + 30 * 60 * 1000).toISOString(), // 30 minutes
          sellerDisplayName: 'Test Seller',
        },
        {
          id: 'auction-4',
          name: 'Test Auction 4',
          description: 'Test description',
          imageUrl: 'https://example.com/test.jpg',
          currentPrice: 10.0,
          numberOfBids: 0,
          endDate: new Date(mockNow.getTime() - 1000).toISOString(), // Ended
          sellerDisplayName: 'Test Seller',
        },
      ],
    }

    server.use(
      http.get('/api/auctions', () => {
        return HttpResponse.json(testAuctionsResponse)
      })
    )

    renderWithProvidersAndRouter(<CurrentAuctions />)

    await waitFor(() => {
      expect(screen.getByText('2d 0h')).toBeInTheDocument()
      expect(screen.getByText('3h 0m')).toBeInTheDocument()
      expect(screen.getByText('30m')).toBeInTheDocument()
      expect(screen.getByText('Ended')).toBeInTheDocument()
    })

    vi.useRealTimers()
  })

  it('handles singular vs plural bid count correctly', async () => {
    server.use(
      http.get('/api/auctions', () => {
        return HttpResponse.json(mockAuctionsResponse)
      })
    )

    renderWithProvidersAndRouter(<CurrentAuctions />)

    await waitFor(() => {
      expect(screen.getByText('5 bids')).toBeInTheDocument() // plural
      expect(screen.getByText('1 bid')).toBeInTheDocument() // singular
      expect(screen.getByText('0 bids')).toBeInTheDocument() // plural for zero
    })
  })
})
