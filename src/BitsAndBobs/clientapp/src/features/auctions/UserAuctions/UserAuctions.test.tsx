import { screen } from '@testing-library/react'
import { HttpResponse, http } from 'msw'
import { setupServer } from 'msw/node'
import { describe, expect, it } from 'vitest'
import UserAuctions from './UserAuctions'
import {
  renderWithProvidersAndRouter,
  userEvent,
} from '../../../testing/test-utils'

const server = setupServer()

const fakeAuctionsResponse = (overrides = {}) => ({
  auctions: [
    {
      id: 'auction-1',
      name: 'Vintage Camera',
      description: '',
      imageHref: '/img.png',
      currentPrice: 120.5,
      initialPrice: 100,
      endDate: new Date(),
      numberOfBids: 5,
      isOpen: true,
      isClosed: false,
      isCancelled: false,
      isUserCurrentBidder: false,
      isUserSeller: false,
      sellerDisplayName: 'Bill Poppers',
      ...overrides,
    },
  ],
})

const emptyAuctionsResponse = {
  auctions: [],
}

describe('UserAuctions Component', () => {
  beforeAll(() => {
    server.listen()
  })

  afterEach(() => {
    server.resetHandlers()
  })

  afterAll(() => {
    server.close()
  })

  it('renders the component with tabs and shows seller auctions by default', () => {
    server.use(
      http.get('/api/auctions/*', () => {
        return HttpResponse.json(emptyAuctionsResponse)
      })
    )

    renderWithProvidersAndRouter(<UserAuctions />)

    expect(
      screen.getByRole('heading', { name: 'My Auctions' })
    ).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Selling' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Won' })).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: 'Participating' })
    ).toBeInTheDocument()
  })

  it('renders seller auctions by default', async () => {
    server.use(
      http.get('/api/auctions/seller', () => {
        return HttpResponse.json(fakeAuctionsResponse({ name: 'Palm Tree' }))
      })
    )

    renderWithProvidersAndRouter(<UserAuctions />)

    expect(screen.getByRole('button', { name: 'Selling' })).toHaveAttribute(
      'aria-current',
      'page'
    )
    expect(await screen.findByText('Palm Tree')).toBeInTheDocument()
  })

  it('switches to Won tab and shows won auctions', async () => {
    server.use(
      http.get('/api/auctions/seller', () => {
        return HttpResponse.json(fakeAuctionsResponse({ name: 'My Soul' }))
      }),
      http.get('/api/auctions/won', () => {
        return HttpResponse.json(
          fakeAuctionsResponse({ name: "Kurt's Jacket" })
        )
      })
    )

    renderWithProvidersAndRouter(<UserAuctions />)
    const wonTab = screen.getByRole('button', { name: 'Won' })
    await userEvent.click(wonTab)

    expect(wonTab).toHaveAttribute('aria-current', 'page')
    expect(await screen.findByText("Kurt's Jacket")).toBeInTheDocument()
    expect(screen.queryByText('My Soul')).not.toBeInTheDocument()
  })

  it('switches to Participating tab and shows participating auctions', async () => {
    server.use(
      http.get('/api/auctions/seller', () => {
        return HttpResponse.json(fakeAuctionsResponse({ name: 'My Soul' }))
      }),
      http.get('/api/auctions/participant', () => {
        return HttpResponse.json(fakeAuctionsResponse({ name: 'Aged Crab' }))
      })
    )

    renderWithProvidersAndRouter(<UserAuctions />)
    const participatingTab = screen.getByRole('button', {
      name: 'Participating',
    })
    await userEvent.click(participatingTab)

    expect(participatingTab).toHaveAttribute('aria-current', 'page')
    expect(await screen.findByText('Aged Crab')).toBeInTheDocument()
    expect(screen.queryByText('My Soul')).not.toBeInTheDocument()
  })

  it('shows loading state while fetching auctions', async () => {
    server.use(
      http.get('/api/auctions/seller', async () => {
        await new Promise((resolve) => setTimeout(resolve, 100))
        return HttpResponse.json(
          fakeAuctionsResponse({ name: 'Chicken Leather Trousers' })
        )
      })
    )

    renderWithProvidersAndRouter(<UserAuctions />)

    expect(screen.getByRole('status')).toBeInTheDocument()
    expect(
      await screen.findByText('Chicken Leather Trousers')
    ).toBeInTheDocument()
    expect(screen.queryByRole('status')).not.toBeInTheDocument()
  })

  it('shows error message when unable to fetch auctions', async () => {
    server.use(
      http.get('/api/auctions/seller', () => {
        return new HttpResponse(null, { status: 500 })
      })
    )

    renderWithProvidersAndRouter(<UserAuctions />)

    expect(
      await screen.findByText(
        'Unable to load your auctions. Please try again later.'
      )
    ).toBeInTheDocument()
  })

  it('shows empty state message for seller auctions', async () => {
    server.use(
      http.get('/api/auctions/seller', () => {
        return HttpResponse.json(emptyAuctionsResponse)
      })
    )

    renderWithProvidersAndRouter(<UserAuctions />)

    expect(
      await screen.findByText("You haven't created any auctions yet.")
    ).toBeInTheDocument()
    expect(
      screen.getByRole('link', { name: 'Sell Something' })
    ).toHaveAttribute('href', '/auctions/create')
  })

  it('shows empty state for won auctions without create button', async () => {
    server.use(
      http.get('/api/auctions/seller', () => {
        return HttpResponse.json(fakeAuctionsResponse())
      }),
      http.get('/api/auctions/won', () => {
        return HttpResponse.json(emptyAuctionsResponse)
      })
    )

    renderWithProvidersAndRouter(<UserAuctions />)
    const wonTab = screen.getByRole('button', { name: 'Won' })
    await userEvent.click(wonTab)

    expect(
      await screen.findByText("You haven't won any auctions yet.")
    ).toBeInTheDocument()
    expect(
      screen.queryByRole('link', { name: 'Sell Something' })
    ).not.toBeInTheDocument()
  })

  it('shows empty state for participating auctions without create button', async () => {
    server.use(
      http.get('/api/auctions/seller', () => {
        return HttpResponse.json(fakeAuctionsResponse())
      }),
      http.get('/api/auctions/participant', () => {
        return HttpResponse.json(emptyAuctionsResponse)
      })
    )

    renderWithProvidersAndRouter(<UserAuctions />)
    const participatingTab = screen.getByRole('button', {
      name: 'Participating',
    })
    await userEvent.click(participatingTab)

    expect(
      await screen.findByText("You haven't bid on any auctions yet.")
    ).toBeInTheDocument()
    expect(
      screen.queryByRole('link', { name: 'Sell Something' })
    ).not.toBeInTheDocument()
  })
})
