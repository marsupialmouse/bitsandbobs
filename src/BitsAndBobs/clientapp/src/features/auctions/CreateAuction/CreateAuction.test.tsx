import { screen, waitFor } from '@testing-library/react'
import { HttpResponse, http } from 'msw'
import { setupServer } from 'msw/node'
import { describe, expect, it, vi } from 'vitest'
import CreateAuction from './CreateAuction'
import {
  renderWithProvidersAndRouter,
  userEvent,
} from '../../../testing/test-utils'

const mockUploadResponse = {
  id: 'test-image-id-123',
  href: '/test-image.png',
}

const mockCreateAuctionResponse = {
  id: 'auction-123',
}

const server = setupServer(
  http.post('/api/auctions/images', () => {
    return HttpResponse.json(mockUploadResponse)
  })
)

// Mock navigate function
const mockNavigate = vi.fn()
vi.mock('react-router', async () => {
  const actual = await vi.importActual('react-router')
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  }
})

describe('CreateAuction Component', () => {
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

  const fillFormWithValidData = async () => {
    // Fill in the form
    await userEvent.type(screen.getByLabelText('Name'), 'Test Auction')
    await userEvent.type(
      screen.getByLabelText('Description'),
      'Test description'
    )
    await userEvent.clear(screen.getByLabelText('Initial Price ($)'))
    await userEvent.type(screen.getByLabelText('Initial Price ($)'), '10')
    await userEvent.clear(screen.getByLabelText('Bid Increment ($)'))
    await userEvent.type(screen.getByLabelText('Bid Increment ($)'), '1')

    // Upload an image
    const file = new File(['test'], 'test.jpg', { type: 'image/jpeg' })
    const fileInput = screen.getByTestId('image-upload-input')
    await userEvent.upload(fileInput, file)

    await waitFor(() => {
      expect(screen.getByRole('img', { name: 'Preview' })).toBeInTheDocument()
    })
  }

  it('renders create auction form with all fields', () => {
    renderWithProvidersAndRouter(<CreateAuction />)

    expect(
      screen.getByRole('heading', { name: 'Sell Your Bits & Bobs' })
    ).toBeInTheDocument()
    expect(screen.getByLabelText('Name')).toBeInTheDocument()
    expect(screen.getByLabelText('Description')).toBeInTheDocument()
    expect(screen.getByLabelText('Initial Price ($)')).toBeInTheDocument()
    expect(screen.getByLabelText('Bid Increment ($)')).toBeInTheDocument()
    expect(screen.getByLabelText('Days')).toBeInTheDocument()
    expect(screen.getByLabelText('Hours')).toBeInTheDocument()
    expect(screen.getByLabelText('Minutes')).toBeInTheDocument()
    expect(screen.getByText('Auction Image')).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: 'Create Auction' })
    ).toBeInTheDocument()
  })

  it('has correct default values', () => {
    renderWithProvidersAndRouter(<CreateAuction />)

    expect(screen.getByLabelText('Name')).toHaveValue('')
    expect(screen.getByLabelText('Description')).toHaveValue('')
    expect(screen.getByLabelText('Initial Price ($)')).toHaveValue(0)
    expect(screen.getByLabelText('Bid Increment ($)')).toHaveValue(0.1)
    expect(screen.getByLabelText('Days')).toHaveValue(0)
    expect(screen.getByLabelText('Hours')).toHaveValue(0)
    expect(screen.getByLabelText('Minutes')).toHaveValue(10)
  })

  it('has correct default values for relisting auction', () => {
    const auction = {
      name: 'Sounds',
      description: 'Like Sunset',
      imageId: 'test-image-id-7000',
      imageHref: 'https://barry.com/toadfish.png',
      initialPrice: 874.54,
      bidIncrement: 8.99,
    }
    renderWithProvidersAndRouter(<CreateAuction relistAuction={auction} />)

    expect(screen.getByLabelText('Name')).toHaveValue(auction.name)
    expect(screen.getByLabelText('Description')).toHaveValue(
      auction.description
    )
    expect(screen.getByLabelText('Initial Price ($)')).toHaveValue(
      auction.initialPrice
    )
    expect(screen.getByLabelText('Bid Increment ($)')).toHaveValue(
      auction.bidIncrement
    )
    expect(screen.getByLabelText('Days')).toHaveValue(0)
    expect(screen.getByLabelText('Hours')).toHaveValue(0)
    expect(screen.getByLabelText('Minutes')).toHaveValue(10)
    expect(screen.getByRole('img', { name: 'Preview' })).toBeInTheDocument()
    expect(screen.getByRole('img', { name: 'Preview' })).toHaveAttribute(
      'src',
      auction.imageHref
    )
  })

  it('validates required fields', async () => {
    renderWithProvidersAndRouter(<CreateAuction />)

    await userEvent.clear(screen.getByLabelText('Initial Price ($)'))
    await userEvent.clear(screen.getByLabelText('Bid Increment ($)'))
    const submitButton = screen.getByRole('button', { name: 'Create Auction' })
    await userEvent.click(submitButton)

    expect(screen.getByLabelText('Name')).toBeInvalid()
    expect(screen.getByLabelText('Description')).toBeInvalid()
    expect(screen.getByLabelText('Initial Price ($)')).toBeInvalid()
    expect(screen.getByLabelText('Bid Increment ($)')).toBeInvalid()
  })

  it('shows error when no image is selected', async () => {
    renderWithProvidersAndRouter(<CreateAuction />)
    await fillFormWithValidData()
    await userEvent.click(screen.getByTestId('image-upload-remove-button'))

    const submitButton = screen.getByRole('button', { name: 'Create Auction' })
    await userEvent.click(submitButton)

    expect(screen.getByText('Please select an image')).toBeInTheDocument()
  })

  it('validates auction period minimum', async () => {
    renderWithProvidersAndRouter(<CreateAuction />)
    await fillFormWithValidData()
    await userEvent.clear(screen.getByLabelText('Minutes'))
    await userEvent.type(screen.getByLabelText('Minutes'), '5')

    const submitButton = screen.getByRole('button', { name: 'Create Auction' })
    await userEvent.click(submitButton)

    expect(
      screen.getByText(
        'Auction period must be at least 10 minutes and no more than 5 days'
      )
    ).toBeInTheDocument()
  })

  it('validates auction period maximum', async () => {
    renderWithProvidersAndRouter(<CreateAuction />)
    await fillFormWithValidData()

    await userEvent.clear(screen.getByLabelText('Days'))
    await userEvent.type(screen.getByLabelText('Days'), '5')

    const submitButton = screen.getByRole('button', { name: 'Create Auction' })
    await userEvent.click(submitButton)

    expect(
      screen.getByText(
        'Auction period must be at least 10 minutes and no more than 5 days'
      )
    ).toBeInTheDocument()
  })

  it('successfully creates auction and navigates', async () => {
    server.use(
      http.post('/api/auctions', () => {
        return HttpResponse.json(mockCreateAuctionResponse)
      })
    )
    renderWithProvidersAndRouter(<CreateAuction />)
    await fillFormWithValidData()

    // Submit the form
    const submitButton = screen.getByRole('button', { name: 'Create Auction' })
    await userEvent.click(submitButton)

    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith('/auction/auction-123')
    })
  })

  it('successfully relists auction and navigates', async () => {
    server.use(
      http.post('/api/auctions', () => {
        return HttpResponse.json(mockCreateAuctionResponse)
      })
    )
    const auction = {
      name: 'Sounds',
      description: 'Like Sunset',
      imageId: 'test-image-id-7000',
      imageHref: 'https://barry.com/toadfish.png',
      initialPrice: 874.54,
      bidIncrement: 8.99,
    }
    renderWithProvidersAndRouter(<CreateAuction relistAuction={auction} />)

    // Submit the form
    const submitButton = screen.getByRole('button', { name: 'Create Auction' })
    await userEvent.click(submitButton)

    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith('/auction/auction-123')
    })
  })

  it('shows loading state while submitting', async () => {
    server.use(
      http.post('/api/auctions', async () => {
        await new Promise((resolve) => setTimeout(resolve, 100))
        return HttpResponse.json(mockCreateAuctionResponse)
      })
    )
    renderWithProvidersAndRouter(<CreateAuction />)
    await fillFormWithValidData()

    const submitButton = screen.getByRole('button', { name: 'Create Auction' })
    await userEvent.click(submitButton)

    expect(screen.getByText('Creating Auction...')).toBeInTheDocument()
    expect(submitButton).toBeDisabled()
  })

  it('handles API validation errors', async () => {
    server.use(
      http.post('/api/auctions', () => {
        return HttpResponse.json(
          {
            errors: {
              name: ['Name is too long'],
              initialPrice: ['Price must be positive'],
            },
          },
          { status: 400 }
        )
      })
    )
    renderWithProvidersAndRouter(<CreateAuction />)
    await fillFormWithValidData()

    const submitButton = screen.getByRole('button', { name: 'Create Auction' })
    await userEvent.click(submitButton)

    await waitFor(() => {
      expect(screen.getByText('Name is too long')).toBeInTheDocument()
      expect(screen.getByText('Price must be positive')).toBeInTheDocument()
    })
  })
})
