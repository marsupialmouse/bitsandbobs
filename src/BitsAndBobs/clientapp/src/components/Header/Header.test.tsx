import { Header } from './Header.tsx'
import { screen } from '@testing-library/react'
import { renderWithRouter } from '../../testing/test-utils.tsx'

describe('Header', () => {
  it('renders the site name', () => {
    renderWithRouter(<Header />)

    expect(screen.getByText('BITS&BOBS')).toBeInTheDocument()
  })

  it('renders Sign Up and Sign In links when not authenticated', () => {
    renderWithRouter(<Header />)

    expect(screen.getByText('Sign Up')).toBeInTheDocument()
    expect(screen.getByText('Sign In')).toBeInTheDocument()
    expect(screen.queryByText('Profile')).not.toBeInTheDocument()
    expect(screen.queryByText('Sign Out')).not.toBeInTheDocument()
  })
})
