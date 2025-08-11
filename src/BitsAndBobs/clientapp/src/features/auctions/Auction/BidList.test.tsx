import { screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import BidList from './BidList'
import { renderWithProvidersAndRouter } from '../../../testing/test-utils'
import { BidDetails } from '../../../api/ApiGenerated'

describe('BidList Component', () => {
  it('shows empty state when no bids are provided', () => {
    renderWithProvidersAndRouter(<BidList bids={[]} />)

    expect(screen.getByText('Bid History')).toBeInTheDocument()
    expect(
      screen.getByText('No bids have been placed yet.')
    ).toBeInTheDocument()
  })

  it('displays the correct number of bids in the header', () => {
    const mockBids: BidDetails[] = [
      {
        id: 'bid-1',
        bidderDisplayName: 'User1',
        amount: 15.0,
        bidDate: new Date(),
        isUserBid: false,
        isCurrentBid: true,
      },
      {
        id: 'bid-2',
        bidderDisplayName: 'User2',
        amount: 10.0,
        bidDate: new Date(),
        isUserBid: false,
        isCurrentBid: false,
      },
    ]

    renderWithProvidersAndRouter(<BidList bids={mockBids} />)

    expect(screen.getByText('Bid History (2)')).toBeInTheDocument()
  })

  it('renders each bid with correct information', () => {
    const now = new Date()
    const earlierDate = new Date(now.getTime() - 10000)

    const mockBids: BidDetails[] = [
      {
        id: 'bid-1',
        bidderDisplayName: 'TestUser',
        amount: 25.5,
        bidDate: now,
        isUserBid: false,
        isCurrentBid: true,
      },
      {
        id: 'bid-2',
        bidderDisplayName: 'PreviousBidder',
        amount: 20.0,
        bidDate: earlierDate,
        isUserBid: false,
        isCurrentBid: false,
      },
    ]

    renderWithProvidersAndRouter(<BidList bids={mockBids} />)

    // Check first bid
    expect(screen.getByText('TestUser')).toBeInTheDocument()
    expect(screen.getByText('$25.50')).toBeInTheDocument()
    expect(screen.getByText('(Current High Bid)')).toBeInTheDocument()
    expect(
      screen.getByText(
        `${now.toLocaleDateString()} ${now.toLocaleTimeString()}`
      )
    ).toBeInTheDocument()

    // Check second bid
    expect(screen.getByText('PreviousBidder')).toBeInTheDocument()
    expect(screen.getByText('$20.00')).toBeInTheDocument()
    expect(
      screen.getByText(
        `${earlierDate.toLocaleDateString()} ${earlierDate.toLocaleTimeString()}`
      )
    ).toBeInTheDocument()
  })

  it("highlights the user's own bids differently", () => {
    const mockBids: BidDetails[] = [
      {
        id: 'bid-1',
        bidderDisplayName: 'OtherUser',
        amount: 30.0,
        bidDate: new Date(),
        isUserBid: false,
        isCurrentBid: true,
      },
      {
        id: 'bid-2',
        bidderDisplayName: 'CurrentUser',
        amount: 25.0,
        bidDate: new Date(),
        isUserBid: true,
        isCurrentBid: false,
      },
    ]

    renderWithProvidersAndRouter(<BidList bids={mockBids} />)

    // Check user bid has special styling
    expect(screen.getByText('CurrentUser')).toBeInTheDocument()
    expect(screen.getByText('(You)')).toBeInTheDocument()

    // Find all bid containers
    const bidContainers = screen
      .getAllByRole('generic')
      .filter((element) => element.className.includes('rounded-lg p-3'))

    // Ensure we have at least two containers
    expect(bidContainers.length).toBeGreaterThanOrEqual(2)

    // Find the user's bid container (should contain "(You)")
    const userBidContainer = bidContainers.find((container) =>
      container.textContent?.includes('(You)')
    )

    // Check user bid has specific background
    expect(userBidContainer).toHaveClass('bg-indigo-50')
    expect(userBidContainer).toHaveClass('border-indigo-200')
  })

  it('displays both "(You)" and "(Current High Bid)" labels when applicable', () => {
    const mockBids: BidDetails[] = [
      {
        id: 'bid-1',
        bidderDisplayName: 'CurrentUser',
        amount: 35.0,
        bidDate: new Date(),
        isUserBid: true,
        isCurrentBid: true,
      },
    ]

    renderWithProvidersAndRouter(<BidList bids={mockBids} />)

    expect(screen.getByText('CurrentUser')).toBeInTheDocument()
    expect(screen.getByText('(You)')).toBeInTheDocument()
    expect(screen.getByText('(Current High Bid)')).toBeInTheDocument()
  })
})
