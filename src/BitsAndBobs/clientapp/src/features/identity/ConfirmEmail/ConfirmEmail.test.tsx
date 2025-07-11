import { describe, expect, it } from 'vitest'
import { screen } from '@testing-library/react'
import { setupServer } from 'msw/node'
import { http, HttpResponse } from 'msw'
import ConfirmEmail from './ConfirmEmail'
import { renderWithProvidersAndRouter } from '../../../testing/test-utils'

const server = setupServer()

const mockSuccessfulResponse = () => {
  server.use(
    http.get('/api/identity/confirmEmail', () => {
      return HttpResponse.json(null, { status: 200 })
    })
  )
}

const mockFailedResponse = (status: number) => {
  server.use(
    http.get('/api/identity/confirmEmail', () => {
      return HttpResponse.json({ status: status }, { status: status })
    })
  )
}

describe('ConfirmEmail', () => {
  beforeAll(() => {
    server.listen()
  })
  afterEach(() => {
    server.resetHandlers()
  })
  afterAll(() => {
    server.close()
  })

  it('displays loading spinner initially', () => {
    server.use(
      http.get('/api/identity/confirmEmail', async () => {
        await new Promise((resolve) => setTimeout(resolve, 600))
        return HttpResponse.json(null, { status: 200 })
      })
    )

    renderWithProvidersAndRouter(<ConfirmEmail />, {
      initialEntries: ['/confirmemail?userId=hello&code=olleh'],
    })

    const spinner = screen.getByRole('status')
    expect(spinner).toBeInTheDocument()
    expect(spinner).toHaveClass('animate-spin')
  })

  it('displays success message on successful account confirmation', async () => {
    mockSuccessfulResponse()

    renderWithProvidersAndRouter(<ConfirmEmail />, {
      initialEntries: ['/confirmemail?userId=hello&code=olleh'],
    })

    const successMessage = await screen.findByText(
      /thanks for confirming your account/i
    )
    expect(successMessage).toBeInTheDocument()
    expect(screen.queryByRole('status')).not.toBeInTheDocument()
  })

  it('displays success message on successful email confirmation', async () => {
    mockSuccessfulResponse()

    renderWithProvidersAndRouter(<ConfirmEmail />, {
      initialEntries: [
        '/confirmemail?userId=hello&code=olleh&changedEmail=mmmmm',
      ],
    })

    const successMessage = await screen.findByText(
      /thanks for confirming your email address/i
    )
    expect(successMessage).toBeInTheDocument()
    expect(screen.queryByRole('status')).not.toBeInTheDocument()
  })

  it('displays error message when request fails', async () => {
    mockFailedResponse(500)

    renderWithProvidersAndRouter(<ConfirmEmail />, {
      initialEntries: ['/confirmemail?userId=hello&code=olleh'],
    })

    const errorMessage = await screen.findByText(/something went wrong/i)
    expect(errorMessage).toBeInTheDocument()
    expect(screen.queryByRole('status')).not.toBeInTheDocument()
  })

  it('displays error message when account confirmation fails', async () => {
    mockFailedResponse(401)

    renderWithProvidersAndRouter(<ConfirmEmail />, {
      initialEntries: ['/confirmemail?userId=hello&code=olleh'],
    })

    const errorMessage = await screen.findByText(/account confirmation failed/i)
    expect(errorMessage).toBeInTheDocument()
    expect(screen.queryByRole('status')).not.toBeInTheDocument()
  })

  it('displays error message when email confirmation fails', async () => {
    mockFailedResponse(401)

    renderWithProvidersAndRouter(<ConfirmEmail />, {
      initialEntries: [
        '/confirmemail?userId=hello&code=olleh&changedEmail=mmmmm',
      ],
    })

    const errorMessage = await screen.findByText(/email confirmation failed/i)
    expect(errorMessage).toBeInTheDocument()
    expect(screen.queryByRole('status')).not.toBeInTheDocument()
  })

  it('displays error when required parameters are missing', () => {
    renderWithProvidersAndRouter(<ConfirmEmail />, {
      initialEntries: ['/confirmemail'],
    })

    expect(
      screen.getByText(/one or more required parameters are missing/i)
    ).toBeInTheDocument()
    expect(screen.queryByRole('status')).not.toBeInTheDocument()
  })
})
