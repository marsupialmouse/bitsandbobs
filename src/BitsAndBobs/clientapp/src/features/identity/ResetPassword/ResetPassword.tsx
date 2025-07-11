import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useResetPasswordMutation } from './../identityApiSlice'
import { HttpValidationProblemDetails } from '../../../api/ApiGenerated'
import { Link, useSearchParams } from 'react-router'
import ErrorMessage from '../../../components/ErrorMessage.tsx'

interface ResetPasswordForm {
  password: string
  confirmPassword: string
}

export default function ResetPassword() {
  const [resetPassword, { isLoading, isSuccess }] = useResetPasswordMutation()
  const [apiError, setApiError] = useState<string | null>(null)
  const [searchParams] = useSearchParams()
  const email = searchParams.get('email')
  const resetCode = searchParams.get('code')

  const {
    register: registerField,
    handleSubmit,
    watch,
  } = useForm<ResetPasswordForm>({
    shouldUseNativeValidation: true,
    mode: 'onSubmit',
  })

  const onSubmit = async (data: ResetPasswordForm) => {
    try {
      setApiError(null)
      await resetPassword({
        email: email ?? '',
        resetCode: resetCode ?? '',
        newPassword: data.password,
      }).unwrap()
    } catch (error) {
      const validationErrors = error as HttpValidationProblemDetails
      setApiError(
        validationErrors.errors &&
          Object.keys(validationErrors.errors).includes('InvalidToken')
          ? 'The reset code is invalid. The link you clicked may have been corrupted.'
          : 'An unexpected error occurred resetting your password, please try again later'
      )
    }
  }

  if (!email) {
    return (
      <ErrorMessage message="Unable to reset password: no email address provided. Please check your link." />
    )
  }

  if (!resetCode) {
    return (
      <ErrorMessage message="Unable to reset password: no reset code provided. Please check your link." />
    )
  }

  if (isSuccess) {
    return (
      <div className="pt-10">
        <div className="mx-auto mt-8 max-w-2xl rounded-lg bg-white p-6 shadow">
          <h2 className="mb-6 text-2xl font-bold">Reset Password</h2>
          <div className="mb-6 block text-sm font-medium text-gray-400">
            <p>It was a great success!</p>
            <p className="mt-2">
              {' '}
              Please{' '}
              <Link to="/login" className="inline underline hover:no-underline">
                sign in
              </Link>{' '}
              to make the most of your new password.
            </p>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="pt-10">
      <div className="mx-auto mt-8 max-w-md rounded-lg bg-white p-6 shadow">
        <h2 className="mb-6 text-2xl font-bold">Reset Password</h2>
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
              htmlFor="password"
              className="block text-sm font-medium text-gray-700"
            >
              New password
            </label>
            <input
              {...registerField('password', {
                required: 'New password is required',
                minLength: {
                  value: 6,
                  message: 'Your password must be at least 6 characters',
                },
                pattern: {
                  value:
                    /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{6,}$/,
                  message:
                    'Your password must contain uppercase, lowercase, number and special character',
                },
              })}
              type="password"
              id="password"
              className="mt-1 block w-full rounded-md border-gray-300 px-3 py-2 shadow-sm invalid:shadow-red-400"
            />
          </div>

          <div>
            <label
              htmlFor="confirmPassword"
              className="block text-sm font-medium text-gray-700"
            >
              Confirm Password
            </label>
            <input
              {...registerField('confirmPassword', {
                required: 'Please confirm your new password',
                validate: (value) =>
                  value === watch('password') || 'Passwords do not match',
              })}
              type="password"
              id="confirmPassword"
              className="mt-1 block w-full rounded-md border-gray-300 px-3 py-2 shadow-sm invalid:shadow-red-400"
            />
          </div>

          {apiError && <p className="text-sm text-red-400">{apiError}</p>}

          <button
            type="submit"
            disabled={isLoading}
            className="flex w-full justify-center rounded-md border border-transparent bg-indigo-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-indigo-700 focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 focus:outline-none disabled:opacity-50"
          >
            {isLoading ? 'Resetting password...' : 'Reset password'}
          </button>
        </form>
      </div>
    </div>
  )
}
