import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useLoginMutation } from './../identityApiSlice'
import { ProblemDetails } from '../../../api/ApiGenerated'
import { Link, useLocation, useNavigate } from 'react-router'

interface LoginForm {
  email: string
  password: string
}

export default function Login() {
  const [login, { isLoading }] = useLoginMutation()
  const [apiError, setApiError] = useState<string | null>(null)
  const navigate = useNavigate()
  const location = useLocation()

  const { register, handleSubmit } = useForm<LoginForm>({
    shouldUseNativeValidation: true,
    mode: 'onSubmit',
  })

  const navigateTo = useMemo(() => {
    const state = location.state as { from: Location | undefined } | null
    return state?.from ?? '/'
  }, [location])

  const onSubmit = async (data: LoginForm) => {
    try {
      setApiError(null)
      await login({ email: data.email, password: data.password }).unwrap()
      await navigate(navigateTo, { replace: true })
    } catch (error) {
      const problem = error as ProblemDetails
      setApiError(
        problem.status === 401
          ? problem.detail === 'LockedOut'
            ? 'Your account is locked, please try again later'
            : 'Invalid email or password'
          : 'An unexpected error occurred during login, please try again later'
      )
    }
  }

  return (
    <div className="pt-10">
      <div className="mx-auto mt-8 max-w-md rounded-lg bg-white p-6 shadow">
        <h2 className="mb-6 text-2xl font-bold">Sign In</h2>
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

          <div>
            <label
              htmlFor="password"
              className="block text-sm font-medium text-gray-700"
            >
              Password
            </label>
            <input
              {...register('password', {
                required: 'Password is required',
              })}
              type="password"
              id="password"
              className="mt-1 block w-full rounded-md border-gray-300 px-3 py-2 shadow-sm invalid:shadow-red-400"
            />
          </div>

          {apiError && <p className="text-sm text-red-400">{apiError}</p>}

          <button
            type="submit"
            disabled={isLoading}
            className="flex w-full justify-center rounded-md border border-transparent bg-indigo-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-indigo-700 focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 focus:outline-none disabled:opacity-50"
          >
            {isLoading ? 'Signing in...' : 'Sign In'}
          </button>
        </form>
        <div className="block pt-6 pl-1 text-sm font-medium text-gray-400">
          <Link to="/forgotpassword" className="hover:text-gray-600">
            Forgot your password?
          </Link>
        </div>
      </div>
    </div>
  )
}
