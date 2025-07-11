import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useForgotPasswordMutation } from './../identityApiSlice'
import { Link, Navigate } from 'react-router'
import { useSelector } from 'react-redux'
import { selectIsAuthenticated } from '../../usercontext/userContextSlice.ts'

interface ForgotPasswordForm {
  email: string
}

export default function ForgotPassword() {
  const [forgotPassword, { isLoading, isSuccess }] = useForgotPasswordMutation()
  const [apiError, setApiError] = useState<string | null>(null)
  const isAuthenticated = useSelector(selectIsAuthenticated)

  const { register, handleSubmit, watch } = useForm<ForgotPasswordForm>({
    shouldUseNativeValidation: true,
    mode: 'onSubmit',
  })

  const onSubmit = async (data: ForgotPasswordForm) => {
    try {
      setApiError(null)
      await forgotPassword({ email: data.email }).unwrap()
    } catch {
      setApiError('An unexpected error occurred, please try again later')
    }
  }

  if (isAuthenticated) {
    return <Navigate to="/" replace />
  }

  if (isSuccess) {
    return (
      <div className="pt-10">
        <div className="mx-auto mt-8 max-w-md rounded-lg bg-white p-6 shadow">
          <h2 className="mb-6 text-2xl font-bold">Forgot your password?</h2>
          <div className="mb-6 block text-sm font-medium text-gray-400">
            <p>It was a great success!</p>
            <p className="mt-2">
              {' '}
              Please{' '}
              <Link
                to={`/email/recent/a/${watch('email')}`}
                className="inline underline hover:no-underline"
              >
                check your email
              </Link>{' '}
              for the link to reset your password.
            </p>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="pt-10">
      <div className="mx-auto mt-8 max-w-md rounded-lg bg-white p-6 shadow">
        <h2 className="mb-6 text-2xl font-bold">Forgot your password?</h2>
        <div className="mb-6 block text-sm font-medium text-gray-400">
          Enter your email address below and we&apos;ll send you a link to reset
          your password.
        </div>
        <form
          onSubmit={(e) => {
            void (async () => {
              await handleSubmit(onSubmit)(e)
            })()
          }}
          className="space-y-4"
        >
          <div>
            <label
              htmlFor="email"
              className="block text-sm font-medium text-gray-700"
            >
              Email
            </label>
            <input
              {...register('email', {
                required: 'Email is required',
              })}
              type="email"
              id="email"
              className="mt-1 block w-full rounded-md border-gray-300 px-3 py-2 shadow-sm invalid:shadow-red-400"
            />
          </div>

          {apiError && <p className="text-sm text-red-400">{apiError}</p>}

          <button
            type="submit"
            disabled={isLoading}
            className="flex w-full justify-center rounded-md border border-transparent bg-indigo-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-indigo-700 focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 focus:outline-none disabled:opacity-50"
          >
            {isLoading ? 'Sending...' : 'Send'}
          </button>
        </form>
      </div>
    </div>
  )
}
