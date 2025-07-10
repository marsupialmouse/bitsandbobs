import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useRegisterMutation } from './../identityApiSlice'
import { HttpValidationProblemDetails } from '../../../api/ApiGenerated'
import { Link } from 'react-router'

interface RegistrationForm {
  email: string
  password: string
  confirmPassword: string
}

export default function Register() {
  const [register, { isLoading }] = useRegisterMutation()
  const [apiError, setApiError] = useState<string | null>(null)
  const [isRegistered, setIsRegistered] = useState(false)

  const {
    register: registerField,
    handleSubmit,
    watch,
  } = useForm<RegistrationForm>({
    shouldUseNativeValidation: true,
    mode: 'onChange',
  })

  const onSubmit = async (data: RegistrationForm) => {
    try {
      setApiError(null)
      await register({ email: data.email, password: data.password }).unwrap()
      setIsRegistered(true)
    } catch (error) {
      let errorMessage: string | undefined = undefined
      const validationErrors = error as HttpValidationProblemDetails
      if (validationErrors.errors) {
        const validationError = Object.entries(validationErrors.errors).find(
          ([key, messages]) =>
            !key.toLowerCase().includes('username') && messages.length > 0
        )
        if (validationError) {
          errorMessage = validationError[1][0]
        }
      }

      setApiError(errorMessage ?? 'An error occurred during registration')
    }
  }

  if (isRegistered) {
    return (
      <div className="pt-10">
        <div className="mx-auto mt-8 max-w-2xl rounded-lg bg-white p-6 shadow">
          <h2 className="mb-6 text-2xl font-bold">Registration Successful</h2>
          <p className="text-sm text-gray-600">
            Thanks for signing up!
            <Link
              to={`/email/recent/a/${watch('email')}`}
              className="pl-2 underline"
            >
              Check your email
            </Link>{' '}
            and follow the instructions to complete your registration.
          </p>
        </div>
      </div>
    )
  }

  return (
    <div className="pt-10">
      <div className="mx-auto mt-8 max-w-md rounded-lg bg-white p-6 shadow">
        <h2 className="mb-6 text-2xl font-bold">Sign Up</h2>
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
              {...registerField('email', {
                required: 'Email is required',
              })}
              type="email"
              id="email"
              className="mt-1 block w-full rounded-md border-gray-300 px-3 py-2 shadow-sm invalid:shadow-red-400"
            />
          </div>

          <div>
            <label
              htmlFor="password"
              className="block text-sm font-medium text-gray-700"
            >
              Password
            </label>
            <input
              {...registerField('password', {
                required: 'Password is required',
                minLength: {
                  value: 6,
                  message: 'Password must be at least 6 characters',
                },
                pattern: {
                  value:
                    /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{6,}$/,
                  message:
                    'Password must contain uppercase, lowercase, number and special character',
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
                required: 'Please confirm your password',
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
            {isLoading ? 'Signing up...' : 'Sign Up'}
          </button>
        </form>
      </div>
    </div>
  )
}
