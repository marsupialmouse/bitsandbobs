import { screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { renderWithProvidersAndRouter } from '../../../testing/test-utils'
import AuctionList from './AuctionList'
import { addDays, addHours } from 'date-fns'
import formatTimeRemaining from '../formatTimeRemaining.ts'

// Mock sample auctions data
const createMockAuction = (overrides = {}) => ({
  id: 'auction-1',
  name: 'Vintage Camera',
  description: 'A beautiful vintage camera',
  imageHref: 'http://example.com/camera.jpg',
  currentPrice: 120.5,
  initialPrice: 100,
  endDate: addDays(new Date(), 2),
  numberOfBids: 5,
  isOpen: true,
  isClosed: false,
  isCancelled: false,
  isUserCurrentBidder: false,
  isUserSeller: false,
  sellerDisplayName: 'PhotoCollector',
  ...overrides,
})

describe('AuctionList Component', () => {
  it('shows loading state when loading', () => {
    renderWithProvidersAndRouter(
      <AuctionList
        isLoading={true}
        isError={false}
        auctions={undefined}
        emptyMessage="No auctions found"
        errorMessage="Error loading auctions"
      />
    )

    expect(screen.getByRole('status')).toBeInTheDocument()
  })

  it('shows error message when there is an error', () => {
    renderWithProvidersAndRouter(
      <AuctionList
        isLoading={false}
        isError={true}
        auctions={undefined}
        emptyMessage="No auctions found"
        errorMessage="Error loading auctions"
      />
    )

    expect(screen.getByText('Error loading auctions')).toBeInTheDocument()
  })

  it('shows empty message when there are no auctions', () => {
    renderWithProvidersAndRouter(
      <AuctionList
        isLoading={false}
        isError={false}
        auctions={[]}
        emptyMessage="No auctions found"
        errorMessage="Error loading auctions"
      />
    )

    expect(screen.getByText('No auctions found')).toBeInTheDocument()
  })

  it('shows create button when showCreateButton is true', () => {
    renderWithProvidersAndRouter(
      <AuctionList
        isLoading={false}
        isError={false}
        auctions={[]}
        emptyMessage="No auctions found"
        errorMessage="Error loading auctions"
        showCreateButton={true}
      />
    )

    expect(screen.getByText('Sell Something')).toBeInTheDocument()
    expect(screen.getByText('Sell Something').closest('a')).toHaveAttribute(
      'href',
      '/auctions/create'
    )
  })

  it('does not show create button when showCreateButton is false', () => {
    renderWithProvidersAndRouter(
      <AuctionList
        isLoading={false}
        isError={false}
        auctions={[]}
        emptyMessage="No auctions found"
        errorMessage="Error loading auctions"
        showCreateButton={false}
      />
    )

    expect(screen.queryByText('Sell Something')).not.toBeInTheDocument()
  })

  it('displays basic auction details correctly', () => {
    const auction = createMockAuction({
      name: 'Toad Hall',
      currentPrice: 42.75,
      imageHref: 'https://fufme.com/images/mole.png',
    })

    renderWithProvidersAndRouter(
      <AuctionList
        isLoading={false}
        isError={false}
        auctions={[auction]}
        emptyMessage="No auctions found"
        errorMessage="Error loading auctions"
      />
    )

    // Check basic auction data
    expect(screen.getByText(auction.name)).toBeInTheDocument()
    expect(screen.getByText('$42.75')).toBeInTheDocument()
    const cameraImage = screen.getByRole('img', { name: auction.name })
    expect(cameraImage).toBeInTheDocument()
    expect(cameraImage).toHaveAttribute('src', auction.imageHref)
    expect(screen.queryByText('Error loading auctions')).not.toBeInTheDocument()
    expect(screen.queryByText('No auctions found')).not.toBeInTheDocument()
    expect(screen.queryByRole('status')).not.toBeInTheDocument()
  })

  it('renders multiple auctions correctly', () => {
    const auctions = [
      createMockAuction({ id: 'auction-1', name: 'Vintage Camera' }),
      createMockAuction({ id: 'auction-2', name: 'Antique Watch' }),
      createMockAuction({ id: 'auction-3', name: 'Gaming Console' }),
    ]

    renderWithProvidersAndRouter(
      <AuctionList
        isLoading={false}
        isError={false}
        auctions={auctions}
        emptyMessage="No auctions found"
        errorMessage="Error loading auctions"
      />
    )

    expect(screen.getByText('Vintage Camera')).toBeInTheDocument()
    expect(screen.getByText('Antique Watch')).toBeInTheDocument()
    expect(screen.getByText('Gaming Console')).toBeInTheDocument()
  })

  it('displays open auction correctly', () => {
    const endDate = addHours(new Date(), 9)

    const auction = createMockAuction({
      name: 'Open Auction',
      currentPrice: 42.75,
      isOpen: true,
      isClosed: false,
      isCancelled: false,
      endDate: endDate,
    })

    renderWithProvidersAndRouter(
      <AuctionList
        isLoading={false}
        isError={false}
        auctions={[auction]}
        emptyMessage="No auctions found"
        errorMessage="Error loading auctions"
      />
    )

    expect(screen.getByText('Active')).toBeInTheDocument()
    expect(screen.getByText(formatTimeRemaining(endDate))).toBeInTheDocument()
    expect(
      screen.getByText(`(ends ${endDate.toLocaleDateString()})`)
    ).toBeInTheDocument()
    expect(screen.queryByText(/Ended/)).not.toBeInTheDocument()
    expect(screen.queryByText(/Cancelled/)).not.toBeInTheDocument()
  })

  it('displays closed auction correctly', () => {
    const endDate = addHours(new Date(), 13)

    const auction = createMockAuction({
      name: 'Closed Auction',
      currentPrice: 42.75,
      isOpen: false,
      isClosed: true,
      isCancelled: false,
      endDate: endDate,
    })

    renderWithProvidersAndRouter(
      <AuctionList
        isLoading={false}
        isError={false}
        auctions={[auction]}
        emptyMessage="No auctions found"
        errorMessage="Error loading auctions"
      />
    )

    expect(screen.getByText('Ended')).toBeInTheDocument()
    expect(
      screen.getByText(`Ended ${endDate.toLocaleDateString()}`)
    ).toBeInTheDocument()
    expect(
      screen.queryByText(formatTimeRemaining(endDate))
    ).not.toBeInTheDocument()
    expect(screen.queryByText('Active')).not.toBeInTheDocument()
    expect(screen.queryByText(/Cancelled/)).not.toBeInTheDocument()
  })

  it('displays cancelled auction correctly', () => {
    const endDate = addHours(new Date(), 19)
    const cancelledDate = addHours(new Date(), -4)

    const auction = createMockAuction({
      name: 'Cancelled Auction',
      currentPrice: 42.75,
      isOpen: false,
      isClosed: true,
      isCancelled: true,
      endDate: endDate,
      cancelledDate: cancelledDate,
    })

    renderWithProvidersAndRouter(
      <AuctionList
        isLoading={false}
        isError={false}
        auctions={[auction]}
        emptyMessage="No auctions found"
        errorMessage="Error loading auctions"
      />
    )

    expect(screen.getByText('Cancelled')).toBeInTheDocument()
    expect(
      screen.getByText(`Cancelled ${cancelledDate.toLocaleDateString()}`)
    ).toBeInTheDocument()
    expect(
      screen.queryByText(`(ends ${endDate.toLocaleDateString()})`)
    ).not.toBeInTheDocument()
    expect(
      screen.queryByText(formatTimeRemaining(endDate))
    ).not.toBeInTheDocument()
    expect(screen.queryByText('Active')).not.toBeInTheDocument()
    expect(screen.queryByText('Ended')).not.toBeInTheDocument()
  })

  it('displays leading bid badge when user is winning', () => {
    const auction = createMockAuction({
      name: 'User Leading Auction',
      isUserCurrentBidder: true,
    })

    renderWithProvidersAndRouter(
      <AuctionList
        isLoading={false}
        isError={false}
        auctions={[auction]}
        emptyMessage="No auctions found"
        errorMessage="Error loading auctions"
      />
    )

    expect(screen.getByText('Leading Bid')).toBeInTheDocument()
    expect(screen.queryByText('Winner!')).not.toBeInTheDocument()
  })

  it('displays winner badge when user won auction', () => {
    const auction = createMockAuction({
      isUserCurrentBidder: true,
      isClosed: true,
      isOpen: false,
    })

    renderWithProvidersAndRouter(
      <AuctionList
        isLoading={false}
        isError={false}
        auctions={[auction]}
        emptyMessage="No auctions found"
        errorMessage="Error loading auctions"
      />
    )

    expect(screen.getByText('Winner!')).toBeInTheDocument()
    expect(screen.queryByText('Leading Bid')).not.toBeInTheDocument()
  })

  it('displays no winner badge when no bids on closed auction', () => {
    const auction = createMockAuction({
      isUserCurrentBidder: true,
      isClosed: true,
      isOpen: false,
      numberOfBids: 0,
    })

    renderWithProvidersAndRouter(
      <AuctionList
        isLoading={false}
        isError={false}
        auctions={[auction]}
        emptyMessage="No auctions found"
        errorMessage="Error loading auctions"
      />
    )

    expect(screen.getByText('No Winner')).toBeInTheDocument()
    expect(screen.getByText('0 bids')).toBeInTheDocument()
  })

  it('does not display no winner badge when auction not closed', () => {
    const auction = createMockAuction({
      isUserCurrentBidder: true,
      isClosed: false,
      isCancelled: true,
      isOpen: false,
      numberOfBids: 0,
    })

    renderWithProvidersAndRouter(
      <AuctionList
        isLoading={false}
        isError={false}
        auctions={[auction]}
        emptyMessage="No auctions found"
        errorMessage="Error loading auctions"
      />
    )

    expect(screen.getByText('0 bids')).toBeInTheDocument()
    expect(screen.queryByText('No Winner')).not.toBeInTheDocument()
  })

  it('does not display leading or winner bid badges when user is not winning', () => {
    const auction = createMockAuction({
      name: 'User Not Leading Auction',
      isUserCurrentBidder: false,
    })

    renderWithProvidersAndRouter(
      <AuctionList
        isLoading={false}
        isError={false}
        auctions={[auction]}
        emptyMessage="No auctions found"
        errorMessage="Error loading auctions"
      />
    )

    expect(screen.queryByText('Leading Bid')).not.toBeInTheDocument()
    expect(screen.queryByText('Winner!')).not.toBeInTheDocument()
  })

  it('displays user maximum bid when available', () => {
    const auction = createMockAuction({
      userMaximumBid: 130,
    })

    renderWithProvidersAndRouter(
      <AuctionList
        isLoading={false}
        isError={false}
        auctions={[auction]}
        emptyMessage="No auctions found"
        errorMessage="Error loading auctions"
      />
    )

    expect(screen.getByText('Your max: $130.00')).toBeInTheDocument()
  })

  it('does not display user maximum bid when not available', () => {
    const auction = createMockAuction({
      userMaximumBid: undefined,
    })

    renderWithProvidersAndRouter(
      <AuctionList
        isLoading={false}
        isError={false}
        auctions={[auction]}
        emptyMessage="No auctions found"
        errorMessage="Error loading auctions"
      />
    )

    expect(screen.queryByText(/Your max:/)).not.toBeInTheDocument()
  })

  it('provides links to auction details', () => {
    const auctions = [
      createMockAuction({ id: 'bob-from-teachers', name: 'Daddy #1 Mug' }),
    ]

    renderWithProvidersAndRouter(
      <AuctionList
        isLoading={false}
        isError={false}
        auctions={auctions}
        emptyMessage="No auctions found"
        errorMessage="Error loading auctions"
      />
    )

    // This returns both the name link and the image link (because of alt text)
    const nameAndImageLinks = screen.getAllByRole('link', {
      name: 'Daddy #1 Mug',
    })
    expect(nameAndImageLinks).toHaveLength(2)
    expect(nameAndImageLinks[0]).toHaveAttribute(
      'href',
      '/auction/bob-from-teachers'
    )
    expect(nameAndImageLinks[1]).toHaveAttribute(
      'href',
      '/auction/bob-from-teachers'
    )
    const detailsLink = screen.getByText('View Details')
    expect(detailsLink).toHaveAttribute('href', '/auction/bob-from-teachers')
  })
})
