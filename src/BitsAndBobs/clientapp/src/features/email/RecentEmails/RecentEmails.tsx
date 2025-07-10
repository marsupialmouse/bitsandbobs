import { Link } from 'react-router'
import { JSX } from 'react'
import ErrorMessage from '../../../components/ErrorMessage.tsx'
import NoContent from '../../../components/NoContent.tsx'
import Loading from '../../../components/Loading.tsx'
import { EmailResponse } from '../../../api/ApiGenerated.ts'

export interface RecentEmailProps {
  heading: string
  emails: EmailResponse[] | undefined
  isLoading: boolean
  isError: boolean
}
export default function RecentEmails({
  heading,
  emails,
  isLoading,
  isError,
}: RecentEmailProps) {
  if (isLoading) {
    return <Loading />
  }

  if (isError) {
    return <ErrorMessage message="Error loading recent emails" />
  }

  if (!emails?.length) {
    return <NoContent message="No recent emails found" />
  }

  return (
    <div className="container mx-auto pt-6">
      <h2 className="mb-4 text-xl font-semibold">{heading}</h2>
      <div className="space-y-4">
        {emails.map((email) => (
          <div
            key={`${email.sentAt.toString()}-${email.type}`}
            className="rounded-lg bg-white p-4 shadow"
          >
            <div className="flex items-start justify-between">
              <div>
                <h3 className="font-medium">{email.type}</h3>
                <p className="text-sm text-gray-600">
                  Sent to: {email.recipient}
                </p>
                <p className="text-sm text-gray-600">
                  Sent at: {new Date(email.sentAt).toLocaleString()}
                </p>
              </div>
            </div>
            <div className="mt-2 text-gray-700">
              {BodyWithLinks({ body: email.body })}
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}

function BodyWithLinks({ body }: { body: string }) {
  // Split the body into lines
  const lines = body.split('\n')

  const processLine = (line: string) => {
    const linkRegex = /\[([^\]]+)]\(([^)]+)\)/g
    const parts: (string | JSX.Element)[] = []
    let lastIndex = 0
    let match

    while ((match = linkRegex.exec(line)) !== null) {
      // Add text before the link
      if (match.index > lastIndex) {
        parts.push(line.slice(lastIndex, match.index))
      }

      parts.push(
        <Link
          key={match.index}
          to={match[2]}
          className="text-blue-600 hover:underline"
        >
          {match[1]}
        </Link>
      )

      lastIndex = match.index + match[0].length
    }

    // Add remaining text after last link
    if (lastIndex < line.length) {
      parts.push(line.slice(lastIndex))
    }

    return parts
  }

  return (
    <>
      {lines.map((line, index) => (
        <p key={index}>{processLine(line)}</p>
      ))}
    </>
  )
}
