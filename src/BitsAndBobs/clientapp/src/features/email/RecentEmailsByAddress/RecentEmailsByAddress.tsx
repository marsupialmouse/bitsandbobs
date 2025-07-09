import { useParams } from 'react-router'
import { useGetRecentEmailsQuery } from '../emailApiSlice'
import ErrorMessage from '../../../components/ErrorMessage.tsx'
import RecentEmails from '../RecentEmails/RecentEmails.tsx'

export default function RecentEmailsByAddress() {
  const { emailAddress } = useParams<{ emailAddress: string }>()
  const {
    data: emails,
    isLoading,
    isError,
  } = useGetRecentEmailsQuery(emailAddress ?? '')

  if (!emailAddress) {
    return (
      <ErrorMessage message="Email address is required to view recent emails." />
    )
  }

  return (
    <RecentEmails
      heading={`Recent emails for ${emailAddress}`}
      emails={emails}
      isLoading={isLoading}
      isError={isError}
    />
  )
}
