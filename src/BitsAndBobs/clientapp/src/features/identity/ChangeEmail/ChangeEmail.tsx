import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useSelector } from 'react-redux'
import { Link } from 'react-router'
import { useChangeEmailMutation } from './../identityApiSlice'
import { selectCurrentEmailAddress } from '../../usercontext/userContextSlice'

interface ChangeEmailForm {
  newEmail: string
}

export default function ChangeEmail() {
  const [changeEmail, { isLoading, isSuccess }] = useChangeEmailMutation()
  const [apiError, setApiError] = useState<string | null>(null)
  const currentEmail = useSelector(selectCurrentEmailAddress)

  const {
    register: registerField,
    handleSubmit,
    reset,
  } = useForm<ChangeEmailForm>({
    shouldUseNativeValidation: true,
    mode: 'onSubmit',
  })

  const onSubmit = async (data: ChangeEmailForm) => {
    try {
      setApiError(null)
      await changeEmail({
        newEmail: data.newEmail,
      }).unwrap()
      reset()
    } catch {
      setApiError(
        'An unexpected error occurred changing your email address, please try again later'
      )
    }
  }

  if (isSuccess) {
    return (
      <div className="pt-10">
        <div className="mx-auto mt-8 max-w-2xl rounded-lg bg-white p-6 shadow">
          <h2 className="mb-6 text-2xl font-bold">Change Email Address</h2>
          <div className="mb-6 block text-sm font-medium text-gray-400">
            It was a great success!
            <Link to="/email/recent/u" className="pl-2 underline">
              Check your email
            </Link>{' '}
            and follow the instructions to confirm your new email address.
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="pt-10">
      <div className="mx-auto mt-8 max-w-md rounded-lg bg-white p-6 shadow">
        <h2 className="mb-6 text-2xl font-bold">Change Email Address</h2>
        <form
          onSubmit={(e) => {
            void (async () => {
              await handleSubmit(onSubmit)(e)
            })()
          }}
          className="space-y-4"
        >
          <div>
            <label className="block text-sm font-medium text-gray-700">
              Current email address
            </label>
            <p className="mt-1 text-sm text-gray-900">{currentEmail}</p>
          </div>

          <div>
            <label
              htmlFor="newEmail"
              className="block text-sm font-medium text-gray-700"
            >
              New email address
            </label>
            <input
              {...registerField('newEmail', {
                required: 'New email address is required',
                pattern: {
                  value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
                  message: 'Please enter a valid email address',
                },
                validate: (value) =>
                  value !== currentEmail ||
                  'Please enter a different email address',
              })}
              type="email"
              id="newEmail"
              className="mt-1 block w-full rounded-md border-gray-300 px-3 py-2 shadow-sm invalid:shadow-red-400"
            />
          </div>

          {apiError && <p className="text-sm text-red-400">{apiError}</p>}

          <button
            type="submit"
            disabled={isLoading}
            className="focus:ring2 flex w-full justify-center rounded-md border border-transparent bg-indigo-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-indigo-700 focus:ring-indigo-500 focus:ring-offset-2 focus:outline-none disabled:opacity-50"
          >
            {isLoading ? 'Changing email address...' : 'Change email address'}
          </button>
        </form>
      </div>
    </div>
  )
}
