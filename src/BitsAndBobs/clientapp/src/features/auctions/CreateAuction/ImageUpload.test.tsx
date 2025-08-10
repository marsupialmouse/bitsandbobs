import { screen, waitFor } from '@testing-library/react'
import { HttpResponse, http } from 'msw'
import { setupServer } from 'msw/node'
import { describe, expect, it, vi } from 'vitest'
import ImageUpload from './ImageUpload'
import {
  renderWithProvidersAndRouter,
  userEvent,
} from '../../../testing/test-utils'

const server = setupServer()

describe('ImageUpload Component', () => {
  beforeAll(() => {
    server.listen()
    // Mock URL.createObjectURL and URL.revokeObjectURL
    global.URL.createObjectURL = vi.fn(() => 'mock-blob-url')
    global.URL.revokeObjectURL = vi.fn()
  })

  afterEach(() => {
    server.resetHandlers()
    vi.clearAllMocks()
  })

  afterAll(() => {
    server.close()
  })

  const mockProps = {
    onImageUploaded: vi.fn(),
    onImageRemoved: vi.fn(),
  }

  it('renders upload area initially', () => {
    renderWithProvidersAndRouter(<ImageUpload {...mockProps} />)

    expect(screen.getByText('Auction Image')).toBeInTheDocument()
    expect(screen.getByText('Upload an image')).toBeInTheDocument()
    expect(screen.getByText('JPEG, PNG, WebP up to 1MB')).toBeInTheDocument()
    expect(screen.getByTestId('image-upload-input')).toBeInTheDocument()
  })

  it('displays external error when provided', () => {
    const errorMessage = 'Please select an image'

    renderWithProvidersAndRouter(
      <ImageUpload {...mockProps} error={errorMessage} />
    )

    expect(screen.getByText(errorMessage)).toBeInTheDocument()
    expect(screen.getByText(errorMessage)).toHaveClass('text-red-600')
  })

  it('handles successful image upload', async () => {
    server.use(
      http.post('/api/auctions/images', () => {
        return HttpResponse.json({ id: 'success-image' })
      })
    )
    const file = new File(['test'], 'test.jpg', { type: 'image/jpeg' })

    renderWithProvidersAndRouter(<ImageUpload {...mockProps} />)
    const fileInput = screen.getByTestId('image-upload-input')
    await userEvent.upload(fileInput, file)

    await waitFor(() => {
      expect(mockProps.onImageUploaded).toHaveBeenCalledWith(
        'test-image-id-123'
      )
    })
    expect(screen.getByRole('img', { name: 'Preview' })).toBeInTheDocument()
    expect(screen.getByRole('img', { name: 'Preview' })).toHaveAttribute(
      'src',
      'mock-blob-url'
    )
  })

  it('shows uploading state during upload', async () => {
    server.use(
      http.post('/api/auctions/images', async () => {
        await new Promise((resolve) => setTimeout(resolve, 100))
        return HttpResponse.json({ id: 'uploading-state-image' })
      })
    )
    const file = new File(['test'], 'test.jpg', { type: 'image/jpeg' })

    renderWithProvidersAndRouter(<ImageUpload {...mockProps} />)
    const fileInput = screen.getByTestId('image-upload-input')
    await userEvent.upload(fileInput, file)

    expect(screen.getByRole('status')).toBeInTheDocument()
    expect(fileInput).toBeDisabled()
  })

  it('clears file input after uploading', async () => {
    server.use(
      http.post('/api/auctions/images', () =>
        HttpResponse.json({ id: 'clears-file-image' })
      )
    )
    const file = new File(['test'], 'test.jpg', { type: 'image/jpeg' })

    renderWithProvidersAndRouter(<ImageUpload {...mockProps} />)
    const fileInput = screen.getByTestId('image-upload-input')
    await userEvent.upload(fileInput, file)

    await waitFor(() => {
      expect(mockProps.onImageUploaded).toHaveBeenCalled()
    })
    expect(fileInput).toHaveValue('')
  })

  it('rejects files larger than 1MB', async () => {
    const largeFile = new File(['x'.repeat(1024 * 1024 + 1)], 'large.jpg', {
      type: 'image/jpeg',
    })

    renderWithProvidersAndRouter(<ImageUpload {...mockProps} />)
    const fileInput = screen.getByTestId('image-upload-input')
    await userEvent.upload(fileInput, largeFile)

    expect(
      await screen.findByText('Image must be smaller than 1MB')
    ).toBeInTheDocument()
    expect(mockProps.onImageUploaded).not.toHaveBeenCalled()
  })

  it('handles upload API errors with validation details', async () => {
    server.use(
      http.post('/api/auctions/images', () => {
        return HttpResponse.json(
          {
            errors: {
              image: ['Image format not supported'],
            },
          },
          { status: 400 }
        )
      })
    )
    const file = new File(['test'], 'test.jpeg', { type: 'image/jpeg' })

    renderWithProvidersAndRouter(<ImageUpload {...mockProps} />)
    const fileInput = screen.getByTestId('image-upload-input')
    await userEvent.upload(fileInput, file)

    expect(
      await screen.findByText('Image format not supported')
    ).toBeInTheDocument()
    expect(mockProps.onImageUploaded).not.toHaveBeenCalled()
  })

  it('handles generic upload API errors', async () => {
    server.use(
      http.post('/api/auctions/images', () => {
        return new HttpResponse(null, { status: 500 })
      })
    )
    const file = new File(['test'], 'test.jpg', { type: 'image/jpeg' })

    renderWithProvidersAndRouter(<ImageUpload {...mockProps} />)
    const fileInput = screen.getByTestId('image-upload-input')
    await userEvent.upload(fileInput, file)

    expect(
      await screen.findByText('Failed to upload image. Please try again.')
    ).toBeInTheDocument()
    expect(mockProps.onImageUploaded).not.toHaveBeenCalled()
  })

  it('allows removing uploaded image', async () => {
    server.use(
      http.post('/api/auctions/images', () => {
        return HttpResponse.json({ id: 'removing-image' })
      })
    )
    const file = new File(['test'], 'test.jpg', { type: 'image/jpeg' })

    renderWithProvidersAndRouter(<ImageUpload {...mockProps} />)
    const fileInput = screen.getByTestId('image-upload-input')
    await userEvent.upload(fileInput, file)

    expect(
      await screen.findByRole('img', { name: 'Preview' })
    ).toBeInTheDocument()
    const removeButton = screen.getByTestId('image-upload-remove-button')
    await userEvent.click(removeButton)

    expect(mockProps.onImageRemoved).toHaveBeenCalled()
    expect(screen.getByText('Upload an image')).toBeInTheDocument()
    expect(
      screen.queryByRole('img', { name: 'Preview' })
    ).not.toBeInTheDocument()
  })

  it('disables file input while uploading', async () => {
    server.use(
      http.post('/api/auctions/images', async () => {
        await new Promise((resolve) => setTimeout(resolve, 100))
        return HttpResponse.json({ id: 'disables-input-image' })
      })
    )
    const file = new File(['test'], 'test.jpg', { type: 'image/jpeg' })

    renderWithProvidersAndRouter(<ImageUpload {...mockProps} />)
    const fileInput = screen.getByTestId('image-upload-input')
    await userEvent.upload(fileInput, file)

    expect(fileInput).toBeDisabled()
  })
})
