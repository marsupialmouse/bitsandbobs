import { screen } from '@testing-library/react'
import { HttpResponse, http } from 'msw'
import { setupServer } from 'msw/node'
import { describe, expect, it, vi } from 'vitest'
import { renderWithProvidersAndRouter } from '../../../testing/test-utils.tsx'
import RelistAuction from './RelistAuction.tsx'

// Mock useParams
vi.mock('react-router', async () => {
  const actual = await vi.importActual('react-router')
  return {
    ...actual,
    useParams: () => ({ id: 'auction-123' }),
  }
})

const mockAuction = {
  name: 'Sounds',
  description: 'Like Sunset',
  imageId: 'test-image-id-7000',
  imageHref: 'https://barry.com/toadfish.png',
  initialPrice: 874.54,
  bidIncrement: 8.99,
}

const server = setupServer()
describe('Relist Auction Component', () => {
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
      http.post('/api/auctions/auction-123/relist', async () => {
        await new Promise((resolve) => setTimeout(resolve, 100))
        return HttpResponse.json(mockAuction)
      })
    )

    renderWithProvidersAndRouter(<RelistAuction />)

    expect(screen.getByRole('status')).toBeInTheDocument()
  })

  it('displays error message when auction fetch fails', async () => {
    server.use(
      http.post('/api/auctions/auction-123/relist', () => {
        return new HttpResponse(null, { status: 500 })
      })
    )

    renderWithProvidersAndRouter(<RelistAuction />)

    expect(
      await screen.findByText(/Unable to load auction details/i)
    ).toBeInTheDocument()
  })

  it('displays auction not found message for 404 error', async () => {
    server.use(
      http.post('/api/auctions/auction-123/relist', () => {
        return HttpResponse.json(null, { status: 404 })
      })
    )

    renderWithProvidersAndRouter(<RelistAuction />)

    expect(await screen.findByText('Auction not found')).toBeInTheDocument()
    expect(
      screen.getByText("The auction you're looking for doesn't exist.")
    ).toBeInTheDocument()
  })

  it('displays relist rules message for 400 error', async () => {
    server.use(
      http.post('/api/auctions/auction-123/relist', () => {
        return HttpResponse.json({ status: 400, type: 'Bad!' }, { status: 400 })
      })
    )

    renderWithProvidersAndRouter(<RelistAuction />)

    expect(
      await screen.findByText('Unable to Relist Auction')
    ).toBeInTheDocument()
    expect(
      screen.getByText(
        'You can only relist your own auctions that are cancelled or complete.'
      )
    ).toBeInTheDocument()
  })

  it('renders the create auction form when auction retrieved successfully', async () => {
    server.use(
      http.post('/api/auctions/auction-123/relist', () =>
        HttpResponse.json(mockAuction)
      )
    )

    renderWithProvidersAndRouter(<RelistAuction />)

    expect(await screen.findByText('Sell Your Bits & Bobs')).toBeInTheDocument()
    expect(screen.getByLabelText('Name')).toHaveValue(mockAuction.name)
    expect(
      screen.getByRole('button', { name: 'Create Auction' })
    ).toBeInTheDocument()
  })
})
