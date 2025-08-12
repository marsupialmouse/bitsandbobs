import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useNavigate } from 'react-router'
import { HttpValidationProblemDetails } from '../../../api/ApiGenerated'
import { useCreateAuctionMutation } from '../auctionsApiSlice.ts'
import ImageUpload from './ImageUpload.tsx'

interface FormData {
  name: string
  description: string
  initialPrice: number
  bidIncrement: number
  days: number
  hours: number
  minutes: number
}

export default function CreateAuction() {
  const navigate = useNavigate()
  const [createAuction] = useCreateAuctionMutation()
  const [uploadedImageId, setUploadedImageId] = useState<string | null>(null)

  const {
    register: registerField,
    handleSubmit,
    setError,
    clearErrors,
    formState: { errors, isSubmitting },
  } = useForm<FormData>({
    shouldUseNativeValidation: true,
    mode: 'onSubmit',
    defaultValues: {
      name: '',
      description: '',
      initialPrice: 0,
      bidIncrement: 0.1,
      days: 0,
      hours: 0,
      minutes: 10,
    },
  })

  const handleImageUploaded = (imageId: string) => {
    setUploadedImageId(imageId)
    clearErrors('root.imageId')
  }

  const handleImageRemoved = () => {
    setUploadedImageId(null)
  }

  const formatTimeSpan = (
    days: number,
    hours: number,
    minutes: number
  ): string => {
    return `${days.toString()}.${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:00`
  }

  const onSubmit = async (data: FormData) => {
    if (!uploadedImageId) {
      setError('root.imageId', { message: 'Please select an image' })
      return
    }

    const totalMinutes = data.days * 24 * 60 + data.hours * 60 + data.minutes
    if (totalMinutes < 10 || totalMinutes > 5 * 24 * 60) {
      setError('root.period', {
        message:
          'Auction period must be at least 10 minutes and no more than 5 days',
      })
      return
    }

    try {
      const result = await createAuction({
        name: data.name,
        description: data.description,
        imageId: uploadedImageId,
        initialPrice: data.initialPrice,
        bidIncrement: data.bidIncrement,
        period: formatTimeSpan(data.days, data.hours, data.minutes),
      }).unwrap()

      await navigate(`/auction/${result.id}`)
    } catch (error) {
      const validationErrors = error as HttpValidationProblemDetails
      if (validationErrors.errors) {
        Object.entries(validationErrors.errors).forEach(([key, messages]) => {
          setError(key as keyof FormData, { message: messages[0] })
        })
      }
    }
  }

  return (
    <div className="mx-auto max-w-2xl p-6">
      <h1 className="mb-6 text-2xl font-bold">Sell Your Bits & Bobs</h1>

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
            htmlFor="name"
            className="block text-sm font-medium text-gray-700"
          >
            Name
          </label>
          <input
            type="text"
            id="name"
            {...registerField('name', {
              required: 'Name is required',
              maxLength: {
                value: 200,
                message: 'Name must be 200 characters or less',
              },
            })}
            className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 focus:outline-none sm:text-sm"
          />
          {errors.name && (
            <p className="mt-1 text-sm text-red-600">{errors.name.message}</p>
          )}
        </div>

        <ImageUpload
          onImageUploaded={handleImageUploaded}
          onImageRemoved={handleImageRemoved}
          error={errors.root?.imageId ? errors.root.imageId.message : ''}
        />

        <div>
          <label
            htmlFor="description"
            className="block text-sm font-medium text-gray-700"
          >
            Description
          </label>
          <textarea
            id="description"
            {...registerField('description', {
              required: 'Description is required',
              maxLength: {
                value: 2000,
                message: 'Description must be 2000 characters or less',
              },
            })}
            rows={4}
            className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 focus:outline-none sm:text-sm"
          />
          {errors.description && (
            <p className="mt-1 text-sm text-red-600">
              {errors.description.message}
            </p>
          )}
        </div>

        <div>
          <label
            htmlFor="initialPrice"
            className="block text-sm font-medium text-gray-700"
          >
            Initial Price ($)
          </label>
          <input
            type="number"
            id="initialPrice"
            step="0.01"
            {...registerField('initialPrice', {
              valueAsNumber: true,
              required: 'Initial price is required',
              min: {
                value: 0.01,
                message: 'Initial price must be greater than zero',
              },
            })}
            className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 focus:outline-none sm:text-sm"
          />
          {errors.initialPrice && (
            <p className="mt-1 text-sm text-red-600">
              {errors.initialPrice.message}
            </p>
          )}
        </div>

        <div>
          <label
            htmlFor="bidIncrement"
            className="block text-sm font-medium text-gray-700"
          >
            Bid Increment ($)
          </label>
          <input
            type="number"
            id="bidIncrement"
            step="0.01"
            {...registerField('bidIncrement', {
              valueAsNumber: true,
              required: 'Bid increment is required',
              min: {
                value: 0.1,
                message: 'Bid increment must be at least 10 cents',
              },
            })}
            className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 focus:outline-none sm:text-sm"
          />
          {errors.bidIncrement && (
            <p className="mt-1 text-sm text-red-600">
              {errors.bidIncrement.message}
            </p>
          )}
        </div>

        <div>
          <label className="mb-2 block text-sm font-medium text-gray-700">
            Auction Duration
          </label>
          <div className="grid grid-cols-3 gap-3">
            <div>
              <label
                htmlFor="days"
                className="block text-xs font-medium text-gray-500"
              >
                Days
              </label>
              <input
                type="number"
                id="days"
                {...registerField('days', {
                  valueAsNumber: true,
                  min: { value: 0, message: 'Days cannot be negative' },
                  max: { value: 5, message: 'Maximum 5 days' },
                })}
                className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 focus:outline-none sm:text-sm"
              />
            </div>
            <div>
              <label
                htmlFor="hours"
                className="block text-xs font-medium text-gray-500"
              >
                Hours
              </label>
              <input
                type="number"
                id="hours"
                {...registerField('hours', {
                  valueAsNumber: true,
                  min: { value: 0, message: 'Hours cannot be negative' },
                  max: { value: 23, message: 'Maximum 23 hours' },
                })}
                className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 focus:outline-none sm:text-sm"
              />
            </div>
            <div>
              <label
                htmlFor="minutes"
                className="block text-xs font-medium text-gray-500"
              >
                Minutes
              </label>
              <input
                type="number"
                id="minutes"
                {...registerField('minutes', {
                  valueAsNumber: true,
                  min: { value: 0, message: 'Minutes cannot be negative' },
                  max: { value: 59, message: 'Maximum 59 minutes' },
                })}
                className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 focus:outline-none sm:text-sm"
              />
            </div>
          </div>
          <p className="mt-1 text-xs text-gray-500">
            Minimum 10 minutes, maximum 5 days
          </p>
          {errors.root?.period && (
            <p className="mt-1 text-sm text-red-600">
              {errors.root.period.message}
            </p>
          )}
        </div>

        <div className="pt-4">
          <button
            type="submit"
            disabled={isSubmitting}
            className="flex w-full justify-center rounded-md border border-transparent bg-indigo-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-indigo-700 focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 focus:outline-none disabled:cursor-not-allowed disabled:opacity-50"
          >
            {isSubmitting ? 'Creating Auction...' : 'Create Auction'}
          </button>
        </div>
      </form>
    </div>
  )
}
