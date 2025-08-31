import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router'
import {
  useGetDetailsQuery,
  useUpdateDetailsMutation,
} from './../identityApiSlice'
import Loading from '../../../components/Loading'
import ErrorMessage from '../../../components/ErrorMessage'
import GetMcpTokenButton from './GetMcpTokenButton.tsx'

interface ProfileForm {
  firstName: string
  lastName: string
  displayName: string
}

export default function Profile() {
  const {
    data: userDetails,
    isLoading: isLoadingDetails,
    error: detailsError,
  } = useGetDetailsQuery()
  const [updateDetails, { isLoading }] = useUpdateDetailsMutation()
  const [apiError, setApiError] = useState<string | null>(null)
  const [showSuccess, setShowSuccess] = useState(false)

  const {
    register: registerField,
    handleSubmit,
    reset,
  } = useForm<ProfileForm>({
    shouldUseNativeValidation: true,
    mode: 'onSubmit',
  })

  // Update form values when userDetails loads
  useEffect(() => {
    if (userDetails) {
      reset({
        firstName: userDetails.firstName ?? '',
        lastName: userDetails.lastName ?? '',
        displayName: userDetails.displayName,
      })
    }
  }, [userDetails, reset])

  const onSubmit = async (data: ProfileForm) => {
    try {
      setApiError(null)
      setShowSuccess(false)
      await updateDetails({
        firstName: data.firstName,
        lastName: data.lastName,
        displayName: data.displayName,
      }).unwrap()
      setShowSuccess(true)
    } catch {
      setApiError(
        'An unexpected error occurred updating your profile, please try again later'
      )
    }
  }

  if (isLoadingDetails) {
    return <Loading />
  }

  if (detailsError) {
    return (
      <ErrorMessage message="Unable to load profile details. Please try again later." />
    )
  }

  return (
    <div className="pt-10">
      <div className="mx-auto mt-8 max-w-md rounded-lg bg-white p-6 shadow">
        <h2 className="mb-6 text-2xl font-bold">Profile</h2>

        {showSuccess && (
          <div className="mb-4 rounded-md bg-green-50 p-4">
            <p className="text-sm text-green-800">
              Your profile has been updated successfully.
            </p>
          </div>
        )}

        <form
          onSubmit={(e) => {
            void handleSubmit(onSubmit)(e)
          }}
          className="space-y-4"
        >
          <div>
            <label
              htmlFor="email"
              className="block text-sm font-medium text-gray-700"
            >
              Email address
            </label>
            <div className="mt-1 flex">
              <input
                type="email"
                id="email"
                value={userDetails?.emailAddress ?? ''}
                disabled
                className="block w-full rounded-l-md border-gray-300 bg-gray-50 px-3 py-2 text-gray-500 shadow-sm"
              />
              <Link
                to="/changeemail"
                className="inline-flex items-center rounded-r-md border border-l-0 border-gray-300 bg-gray-50 px-3 py-2 text-sm text-gray-500 hover:bg-gray-100"
              >
                Change
              </Link>
            </div>
          </div>

          <div>
            <label
              htmlFor="firstName"
              className="block text-sm font-medium text-gray-700"
            >
              First name
            </label>
            <input
              {...registerField('firstName')}
              type="text"
              id="firstName"
              className="mt-1 block w-full rounded-md border-gray-300 px-3 py-2 shadow-sm invalid:shadow-red-400"
            />
          </div>

          <div>
            <label
              htmlFor="lastName"
              className="block text-sm font-medium text-gray-700"
            >
              Last name
            </label>
            <input
              {...registerField('lastName')}
              type="text"
              id="lastName"
              className="mt-1 block w-full rounded-md border-gray-300 px-3 py-2 shadow-sm invalid:shadow-red-400"
            />
          </div>

          <div>
            <label
              htmlFor="displayName"
              className="block text-sm font-medium text-gray-700"
            >
              Display name
            </label>
            <input
              {...registerField('displayName', {
                required: 'Display name is required',
              })}
              type="text"
              id="displayName"
              className="mt-1 block w-full rounded-md border-gray-300 px-3 py-2 shadow-sm invalid:shadow-red-400"
            />
          </div>

          {apiError && <p className="text-sm text-red-400">{apiError}</p>}

          <button
            type="submit"
            disabled={isLoading}
            className="flex w-full justify-center rounded-md border border-transparent bg-indigo-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-indigo-700 focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 focus:outline-none disabled:opacity-50"
          >
            {isLoading ? 'Updating profile...' : 'Update profile'}
          </button>
        </form>
        <div className="mt-6 border-t border-gray-200 pt-6">
          <GetMcpTokenButton />
        </div>
      </div>
    </div>
  )
}
