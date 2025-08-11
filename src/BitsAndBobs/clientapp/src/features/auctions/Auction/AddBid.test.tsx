import { screen, waitFor } from '@testing-library/react'
import { HttpResponse, http } from 'msw'
import { setupServer } from 'msw/node'
import { describe, expect, it, vi } from 'vitest'
import AddBid from './AddBid'
import {
  renderWithProvidersAndRouter,
  userEvent,
} from '../../../testing/test-utils'

const server = setupServer(
  http.post('/api/auctions/auction-123/bids', () => {
    return HttpResponse.json({ id: 'new-bid-id' })
  })
)

const defaultProps = {
  auctionId: 'auction-123',
  minimumBid: 11,
  isUserCurrentBidder: false,
  isFetching: false,
}
describe('AddBid Component', () => {
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

  it('renders with minimum bid value pre-filled', () => {
    const props = { ...defaultProps, minimumBid: 12.4 }

    renderWithProvidersAndRouter(<AddBid {...props} />)

    const bidInput = screen.getByRole('spinbutton')
    expect(bidInput).toHaveValue(12.4)
    expect(screen.getByText('Minimum bid: $12.40')).toBeInTheDocument()
  })

  it('shows loading state when fetching auction data', () => {
    const props = { ...defaultProps, isFetching: true }

    renderWithProvidersAndRouter(<AddBid {...props} />)

    expect(screen.getByText('Refreshing auction data...')).toBeInTheDocument()
    expect(screen.getByRole('spinbutton')).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Place Bid' })).toBeDisabled()
  })

  it('shows user is highest bidder notification when appropriate', () => {
    const props = { ...defaultProps, isUserCurrentBidder: true }

    renderWithProvidersAndRouter(<AddBid {...props} />)

    expect(screen.getByText("You're the highest bidder!")).toBeInTheDocument()
    expect(
      screen.getByText('Placing another bid will increase your maximum bid.')
    ).toBeInTheDocument()
  })

  it('requires minimum bid amount', async () => {
    renderWithProvidersAndRouter(<AddBid {...defaultProps} />)

    const bidInput = screen.getByRole('spinbutton')
    await userEvent.clear(bidInput)
    const bidButton = screen.getByRole('button', { name: 'Place Bid' })
    await userEvent.click(bidButton)

    expect(bidInput).toBeInvalid()
  })

  it('validates minimum bid amount', async () => {
    const props = { ...defaultProps, minimumBid: 29.11 }
    renderWithProvidersAndRouter(<AddBid {...props} />)

    const bidInput = screen.getByRole('spinbutton')
    await userEvent.clear(bidInput)
    await userEvent.type(bidInput, '5')
    const bidButton = screen.getByRole('button', { name: 'Place Bid' })
    await userEvent.click(bidButton)

    expect(bidInput).toBeInvalid()
  })

  it('shows loading state during bid submission', async () => {
    server.use(
      http.post('/api/auctions/auction-123/bids', async () => {
        await new Promise((resolve) => setTimeout(resolve, 100))
        return HttpResponse.json({ id: 'new-bid-id' })
      })
    )
    renderWithProvidersAndRouter(<AddBid {...defaultProps} />)

    const bidButton = screen.getByRole('button', { name: 'Place Bid' })
    await userEvent.click(bidButton)

    expect(screen.getByRole('button', { name: 'Bidding...' })).toBeDisabled()
  })

  it('successfully submits a valid bid', async () => {
    const bidSpy = vi.fn().mockResolvedValue({ id: 'new-bid-id' })
    server.use(
      http.post('/api/auctions/auction-123/bids', async ({ request }) => {
        const json = await request.json()
        bidSpy(json)
        return HttpResponse.json({ id: 'new-bid-id' })
      })
    )
    renderWithProvidersAndRouter(<AddBid {...defaultProps} />)

    const bidInput = screen.getByRole('spinbutton')
    await userEvent.clear(bidInput)
    await userEvent.type(bidInput, '15')
    const bidButton = screen.getByRole('button', { name: 'Place Bid' })
    await userEvent.click(bidButton)

    await waitFor(() => {
      expect(bidSpy).toHaveBeenCalledWith({
        auctionId: 'auction-123',
        amount: 15,
      })
    })
  })

  it('handles auction changed error correctly', async () => {
    server.use(
      http.post('/api/auctions/auction-123/bids', () => {
        return HttpResponse.json(
          {
            title: 'InvalidState',
            status: 400,
            detail: 'Auction state has changed',
          },
          { status: 400 }
        )
      })
    )
    renderWithProvidersAndRouter(<AddBid {...defaultProps} />)

    const bidButton = screen.getByRole('button', { name: 'Place Bid' })
    await userEvent.click(bidButton)

    expect(await screen.findByText('Bid not accepted')).toBeInTheDocument()
    expect(
      screen.getByText('Auction details have changed, please try again.')
    ).toBeInTheDocument()
  })

  it('handles general error correctly', async () => {
    server.use(
      http.post('/api/auctions/auction-123/bids', () => {
        return HttpResponse.json(
          {
            title: 'Error',
            status: 400,
            detail: 'Something went wrong',
          },
          { status: 400 }
        )
      })
    )
    renderWithProvidersAndRouter(<AddBid {...defaultProps} />)

    const bidButton = screen.getByRole('button', { name: 'Place Bid' })
    await userEvent.click(bidButton)

    expect(await screen.findByText('Something went wrong')).toBeInTheDocument()
  })
})
