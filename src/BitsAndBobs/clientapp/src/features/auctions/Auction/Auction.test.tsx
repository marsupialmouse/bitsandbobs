import { screen, waitFor } from '@testing-library/react'
import { HttpResponse, http } from 'msw'
import { setupServer } from 'msw/node'
import { describe, expect, it, vi } from 'vitest'
import Auction from './Auction'
import {
  renderWithProvidersAndRouter,
  userEvent,
} from '../../../testing/test-utils'
import { RootState } from '../../../stores/store'
import { AddBidProps } from './AddBid.tsx'
import { BidDetails } from '../../../api/ApiGenerated.ts'

// Mock components we're not testing
vi.mock('./BidList.tsx', () => ({
  default: ({ bids }: { bids: BidDetails[] }) => (
    <div data-testid="bid-list">Bid List: {bids.length} bids</div>
  ),
}))

vi.mock('./AddBid.tsx', () => ({
  default: (props: AddBidProps) => (
    <div data-testid="add-bid">Add Bid Form (minimum: ${props.minimumBid})</div>
  ),
}))

// Mock useParams
vi.mock('react-router', async () => {
  const actual = await vi.importActual('react-router')
  return {
    ...actual,
    useParams: () => ({ id: 'auction-123' }),
  }
})

const mockAuction = {
  id: 'auction-123',
  name: 'Test Auction',
  description: 'Test Description',
  imageHref: 'http://example.com/test.jpg',
  currentPrice: 15.5,
  initialPrice: 10,
  minimumBid: 16,
  endDate: new Date(Date.now() + 3600000).toISOString(), // 1 hour from now
  numberOfBids: 3,
  isOpen: true,
  isClosed: false,
  isCancelled: false,
  isUserSeller: false,
  isUserCurrentBidder: false,
  sellerDisplayName: 'SellerUser',
  currentBidderDisplayName: 'BidderUser',
  bids: [
    {
      id: 'bid-1',
      amount: 15.5,
      bidderDisplayName: 'BidderUser',
      bidDate: new Date().toISOString(),
      isUserBid: false,
      isCurrentBid: true,
    },
    {
      id: 'bid-2',
      amount: 12,
      bidderDisplayName: 'PreviousBidder',
      bidDate: new Date().toISOString(),
      isUserBid: false,
      isCurrentBid: false,
    },
  ],
}

const server = setupServer(
  http.get('/api/auctions/auction-123', () => {
    return HttpResponse.json(mockAuction)
  })
)

// Helper function to render the component with authentication state
function render(isAuthenticated = true) {
  const preloadedState: Partial<RootState> = {
    userContext: {
      isAuthenticated,
      localTimeOffset: 0,
    },
  }

  return renderWithProvidersAndRouter(<Auction />, { preloadedState })
}

describe('Auction Component', () => {
  beforeAll(() => {
    server.listen()
  })

  afterEach(() => {
    server.resetHandlers()
    vi.clearAllMocks()
  })

  afterAll(() => {
    server.close()
  })

  it('shows loading state initially', () => {
    server.use(
      http.get('/api/auctions/auction-123', async () => {
        await new Promise((resolve) => setTimeout(resolve, 100))
        return HttpResponse.json(mockAuction)
      })
    )

    render()

    expect(screen.getByRole('status')).toBeInTheDocument()
  })

  it('displays error message when auction fetch fails', async () => {
    server.use(
      http.get('/api/auctions/auction-123', () => {
        return new HttpResponse(null, { status: 500 })
      })
    )

    render()

    expect(
      await screen.findByText(/Unable to load auction details/i)
    ).toBeInTheDocument()
  })

  it('displays auction not found message for 404 error', async () => {
    server.use(
      http.get('/api/auctions/auction-123', () => {
        return new HttpResponse(null, { status: 404 })
      })
    )

    render()

    expect(await screen.findByText('Auction not found')).toBeInTheDocument()
    expect(
      screen.getByText("The auction you're looking for doesn't exist.")
    ).toBeInTheDocument()
  })

  it('displays auction details when loaded successfully', async () => {
    render()

    expect(await screen.findByText('Test Auction')).toBeInTheDocument()
    expect(screen.getByText('Test Description')).toBeInTheDocument()
    expect(screen.getByText('$15.50')).toBeInTheDocument() // Current price
    expect(screen.getByText('High bidder: BidderUser')).toBeInTheDocument()
    expect(screen.getByText('3 bids')).toBeInTheDocument()
    expect(screen.getByTestId('bid-list')).toBeInTheDocument()
    expect(screen.getByAltText('Test Auction')).toHaveAttribute(
      'src',
      'http://example.com/test.jpg'
    )
  })

  it('shows appropriate status badge for active auction', async () => {
    render()

    expect(await screen.findByText('Active')).toBeInTheDocument()
    expect(screen.getByText('Active')).toHaveClass('bg-green-100')
  })

  it('shows appropriate status badge for closed auction', async () => {
    const closedAuction = { ...mockAuction, isOpen: false, isClosed: true }
    server.use(
      http.get('/api/auctions/auction-123', () => {
        return HttpResponse.json(closedAuction)
      })
    )

    render()

    expect(await screen.findAllByText('Ended')).toHaveLength(2)
    expect(screen.getAllByText('Ended')[0]).toHaveClass('bg-gray-100')
  })

  it('shows appropriate status badge for cancelled auction', async () => {
    const cancelledAuction = {
      ...mockAuction,
      isOpen: false,
      isClosed: true,
      isCancelled: true,
      cancelledDate: new Date().toISOString(),
    }
    server.use(
      http.get('/api/auctions/auction-123', () => {
        return HttpResponse.json(cancelledAuction)
      })
    )

    render()

    expect(await screen.findAllByText('Cancelled')).toHaveLength(2)
    expect(screen.getAllByText('Cancelled')[0]).toHaveClass('bg-red-100')
  })

  it('shows add bid form for authenticated users who are not sellers', async () => {
    render(true)

    expect(await screen.findByTestId('add-bid')).toBeInTheDocument()
    expect(screen.getByTestId('add-bid')).toHaveTextContent(
      'Add Bid Form (minimum: $16)'
    )
  })

  it('shows message when user is the seller', async () => {
    const sellerAuction = { ...mockAuction, isUserSeller: true }
    server.use(
      http.get('/api/auctions/auction-123', () => {
        return HttpResponse.json(sellerAuction)
      })
    )

    render(true)

    expect(
      await screen.findByText("You can't bid on your own auction, you duffer")
    ).toBeInTheDocument()
    expect(screen.queryByTestId('add-bid')).not.toBeInTheDocument()
  })

  it('shows login/signup buttons for unauthenticated users', async () => {
    render(false)

    expect(
      await screen.findByText('Sign in or create an account to place a bid')
    ).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Sign In' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Sign Up' })).toBeInTheDocument()
    expect(screen.queryByTestId('add-bid')).not.toBeInTheDocument()
  })

  it('shows cancel auction button for sellers of open auctions', async () => {
    const sellerAuction = { ...mockAuction, isUserSeller: true }
    server.use(
      http.get('/api/auctions/auction-123', () => {
        return HttpResponse.json(sellerAuction)
      })
    )

    render(true)

    expect(
      await screen.findByRole('button', { name: 'Cancel Auction' })
    ).toBeInTheDocument()
  })

  it('shows cancelling state and calls API when cancel button is clicked', async () => {
    const confirmSpy = vi
      .spyOn(window, 'confirm')
      .mockImplementation(() => true)
    const cancelSpy = vi.fn()

    const sellerAuction = { ...mockAuction, isUserSeller: true }
    server.use(
      http.get('/api/auctions/auction-123', () => {
        return HttpResponse.json(sellerAuction)
      }),
      http.post('/api/auctions/auction-123/cancel', async () => {
        cancelSpy()
        await new Promise((resolve) => setTimeout(resolve, 100))
        return new HttpResponse(null, { status: 204 })
      })
    )

    render(true)

    // Wait for the component to render
    const cancelButton = await screen.findByRole('button', {
      name: 'Cancel Auction',
    })

    // Click the cancel button
    await userEvent.click(cancelButton)

    // Check confirmation was shown
    expect(confirmSpy).toHaveBeenCalled()

    // Check button shows cancelling state
    expect(screen.getByRole('button', { name: 'Cancelling...' })).toBeDisabled()

    // Check API was called
    await waitFor(() => {
      expect(cancelSpy).toHaveBeenCalled()
    })
  })

  it('shows time remaining for open auctions', async () => {
    render()

    expect(await screen.findByText('Time Remaining')).toBeInTheDocument()
    // We're not testing the exact formatted time string as that's implementation-specific
    expect(screen.getByText(/Ends:/)).toBeInTheDocument()
  })

  it('shows ending time for closed auctions', async () => {
    const closedAuction = { ...mockAuction, isOpen: false, isClosed: true }
    server.use(
      http.get('/api/auctions/auction-123', () => {
        return HttpResponse.json(closedAuction)
      })
    )

    render()

    expect(await screen.findAllByText('Ended')).toHaveLength(2)
    expect(screen.getByText(/Ended:/)).toBeInTheDocument()
  })

  it('shows seller information', async () => {
    render()

    expect(await screen.findByText('Seller Information')).toBeInTheDocument()
    expect(screen.getByText('SellerUser')).toBeInTheDocument()
    // Check for first letter avatar
    const avatarElement = screen.getByText('S')
    expect(avatarElement).toBeInTheDocument()
    expect(avatarElement.closest('div')).toHaveClass('rounded-full')
  })

  it('shows relist auction button for sellers of cancelled auctions', async () => {
    const sellerAuction = {
      ...mockAuction,
      isUserSeller: true,
      isCancelled: true,
      isOpen: false,
    }
    server.use(
      http.get('/api/auctions/auction-123', () => {
        return HttpResponse.json(sellerAuction)
      })
    )

    render(true)

    expect(
      await screen.findByRole('button', { name: 'Relist Item' })
    ).toBeInTheDocument()
  })

  it('shows relist auction button for sellers of completed auctions', async () => {
    const sellerAuction = {
      ...mockAuction,
      isUserSeller: true,
      isClosed: true,
      isOpen: false,
    }
    server.use(
      http.get('/api/auctions/auction-123', () => {
        return HttpResponse.json(sellerAuction)
      })
    )

    render(true)

    expect(
      await screen.findByRole('button', { name: 'Relist Item' })
    ).toBeInTheDocument()
  })

  it('does not show relist auction button for sellers of open auctions', async () => {
    const sellerAuction = { ...mockAuction, isUserSeller: true }
    server.use(
      http.get('/api/auctions/auction-123', () => {
        return HttpResponse.json(sellerAuction)
      })
    )

    render(true)

    expect(await screen.findByText('Test Auction')).toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: 'Relist Item' })
    ).not.toBeInTheDocument()
  })

  it('does not show relist auction button for viewers of completed auctions', async () => {
    const sellerAuction = {
      ...mockAuction,
      isUserSeller: false,
      isClosed: true,
      isOpen: false,
    }
    server.use(
      http.get('/api/auctions/auction-123', () => {
        return HttpResponse.json(sellerAuction)
      })
    )

    render(true)

    expect(await screen.findByText('Test Auction')).toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: 'Relist Item' })
    ).not.toBeInTheDocument()
  })
})
