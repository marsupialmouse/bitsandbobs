import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { describe, expect, it } from 'vitest'
import RecentEmails from './RecentEmails'
import { EmailResponse } from '../../../api/ApiGenerated'

const mockEmails: EmailResponse[] = [
  {
    recipient: 'test@example.com',
    type: 'Welcome Email',
    body: 'Hello!\nClick [here](/test) for more info\nOr check your [settings](/settings)',
    sentAt: new Date('2025-07-09T10:00:00Z'),
  },
  {
    recipient: 'other@example.com',
    type: 'Notification',
    body: 'Simple notification\nNo links here',
    sentAt: new Date('2025-07-09T11:00:00Z'),
  },
]

const renderRecentEmails = (props: {
  heading?: string
  emails?: EmailResponse[]
  isLoading?: boolean
  isError?: boolean
}) => {
  const defaultProps = {
    heading: 'Recent Emails',
    emails: [],
    isLoading: false,
    isError: false,
  }

  return render(<RecentEmails {...defaultProps} {...props} />, {
    wrapper: MemoryRouter,
  })
}

describe('RecentEmails', () => {
  it('displays loading state', () => {
    renderRecentEmails({ isLoading: true })
    expect(screen.getByRole('status')).toBeInTheDocument()
  })

  it('displays error state', () => {
    renderRecentEmails({ isError: true })
    expect(screen.getByText(/Error loading recent emails/)).toBeInTheDocument()
  })

  it('displays empty state when no emails', () => {
    renderRecentEmails({ emails: [] })
    expect(screen.getByText(/No recent emails found/)).toBeInTheDocument()
  })

  it('displays emails with all required information', () => {
    renderRecentEmails({
      heading: 'Test Emails',
      emails: mockEmails,
    })

    // Check heading
    expect(screen.getByText('Test Emails')).toBeInTheDocument()

    // Check first email
    expect(screen.getByText('Welcome Email')).toBeInTheDocument()
    expect(screen.getByText('Sent to: test@example.com')).toBeInTheDocument()

    // Check second email
    expect(screen.getByText('Notification')).toBeInTheDocument()
    expect(screen.getByText('Sent to: other@example.com')).toBeInTheDocument()
  })

  it('displays sent date in local format', () => {
    renderRecentEmails({
      heading: 'Test Emails',
      emails: [mockEmails[0]],
    })

    expect(screen.getByText(/^Sent at: /).textContent).toBe(
      `Sent at: ${mockEmails[0].sentAt.toLocaleString()}`
    )
  })

  it('renders markdown links as router links', () => {
    renderRecentEmails({
      emails: mockEmails,
    })

    const links = screen.getAllByRole('link')
    expect(links).toHaveLength(2)

    expect(links[0]).toHaveAttribute('href', '/test')
    expect(links[0]).toHaveTextContent('here')

    expect(links[1]).toHaveAttribute('href', '/settings')
    expect(links[1]).toHaveTextContent('settings')
  })

  it('preserves line breaks in email body', () => {
    renderRecentEmails({
      emails: mockEmails,
    })

    const paragraphs = screen.getAllByText(/Simple notification|No links here/)
    expect(paragraphs).toHaveLength(2)
  })
})
