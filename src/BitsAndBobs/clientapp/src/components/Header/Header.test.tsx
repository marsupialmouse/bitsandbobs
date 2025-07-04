import { Header } from './Header.tsx'
import { screen } from '@testing-library/react'
import { renderWithProvider } from '../../testing/test-utils.tsx'

describe('Header', () => {
  it('renders the site name', () => {
    renderWithProvider(<Header />)

    expect(screen.getByText('BITS&BOBS')).toBeInTheDocument()
  })

  it('renders Sign Up and Sign In links when not authenticated', () => {
    renderWithProvider(<Header />)

    expect(screen.getByText('Sign Up')).toBeInTheDocument()
    expect(screen.getByText('Sign In')).toBeInTheDocument()
    expect(screen.queryByText('Profile')).not.toBeInTheDocument()
    expect(screen.queryByText('Sign Out')).not.toBeInTheDocument()
  })

  it('renders Profile and Sign Out links when authenticated', () => {
    renderWithProvider(<Header />, {
      preloadedState: {
        identity: {
          isAuthenticated: true,
          emailAddress: 'jam@man.com',
          username: 'gammon',
        },
      },
    })

    expect(screen.getByText('Profile')).toBeInTheDocument()
    expect(screen.getByText('Sign Out')).toBeInTheDocument()
    expect(screen.queryByText('Sign Up')).not.toBeInTheDocument()
    expect(screen.queryByText('Sign In')).not.toBeInTheDocument()
  })
})
