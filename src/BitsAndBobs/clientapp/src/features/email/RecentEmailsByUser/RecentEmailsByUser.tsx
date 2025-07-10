import { useGetRecentUserEmailsQuery } from '../emailApiSlice'
import ErrorMessage from '../../../components/ErrorMessage.tsx'
import RecentEmails from '../RecentEmails/RecentEmails.tsx'
import { useSelector } from 'react-redux'
import { selectIsAuthenticated } from '../../usercontext/userContextSlice.ts'

export default function RecentEmailsByUser() {
  const isAuthenticated = useSelector(selectIsAuthenticated)
  const { data: emails, isLoading, isError } = useGetRecentUserEmailsQuery()

  if (!isAuthenticated) {
    return (
      <ErrorMessage message="You must be signed in to view your recent emails." />
    )
  }

  return (
    <RecentEmails
      heading={`Your recent emails`}
      emails={emails}
      isLoading={isLoading}
      isError={isError}
    />
  )
}
