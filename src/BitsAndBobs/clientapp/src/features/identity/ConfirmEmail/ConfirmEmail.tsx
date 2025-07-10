import { useConfirmEmailMutation } from '../identityApiSlice.ts'
import { useSearchParams } from 'react-router'
import { useEffect, useState } from 'react'
import ErrorMessage from '../../../components/ErrorMessage.tsx'
import Loading from '../../../components/Loading.tsx'
import NoContent from '../../../components/NoContent.tsx'
import { ProblemDetails } from '../../../api/ApiGenerated.ts'

export default function ConfirmEmail() {
  const [searchParams] = useSearchParams()
  const [isComplete, setIsComplete] = useState(false)
  const [apiError, setApiError] = useState<string | null>(null)
  const [confirmEmail] = useConfirmEmailMutation()
  const userId = searchParams.get('userId')
  const code = searchParams.get('code')
  const changedEmail = searchParams.get('changedEmail')
  const isValidRequest = userId && code

  useEffect(() => {
    if (!isValidRequest || isComplete) return

    const apiRequest = async () => {
      await confirmEmail({
        userId,
        code,
        changedEmail: changedEmail ?? undefined,
      }).unwrap()
      setIsComplete(true)
    }

    apiRequest().catch((error: unknown) => {
      const problemDetails = error as ProblemDetails
      setApiError(
        problemDetails.status === 401
          ? `${changedEmail ? 'Email' : 'Account'} confirmation failed. Oh no!`
          : 'Something went wrong, please try again later.'
      )
    })
  }, [isValidRequest, userId, code, changedEmail, confirmEmail, isComplete])

  if (!isValidRequest)
    return (
      <ErrorMessage message="One or more required parameters are missing" />
    )

  if (apiError) return <ErrorMessage message={apiError} />

  if (!isComplete) return <Loading />

  if (changedEmail)
    return <NoContent message="Thanks for confirming your email address!" />

  return (
    <NoContent message="Thanks for confirming your account! Now you can login to buy or sell bits and bobs." />
  )
}
