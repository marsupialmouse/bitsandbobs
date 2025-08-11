import { useAddBidMutation } from '../auctionsApiSlice.ts'
import { ProblemDetails } from '../../../api/ApiGenerated.ts'
import { useForm } from 'react-hook-form'
import { useEffect } from 'react'

interface BidFormData {
  amount: number
}

interface AddBidProps {
  auctionId: string
  minimumBid: number
  isUserCurrentBidder: boolean
  isFetching: boolean
}

export default function AddBid({
  auctionId,
  minimumBid,
  isUserCurrentBidder,
  isFetching,
}: AddBidProps) {
  const [addBid, { isLoading: isPlacingBid }] = useAddBidMutation()

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors },
    setError,
    clearErrors,
  } = useForm<BidFormData>({
    shouldUseNativeValidation: true,
    mode: 'onSubmit',
  })

  // Initialize bid amount to minimum bid
  useEffect(() => {
    if (minimumBid) {
      setValue('amount', minimumBid)
    }
  }, [minimumBid, setValue])

  const onSubmitBid = async (data: BidFormData) => {
    clearErrors()
    try {
      await addBid({
        auctionId: auctionId,
        amount: data.amount,
      }).unwrap()
    } catch (error) {
      console.error('Failed to place bid:', error)
      const problemDetails = error as ProblemDetails

      if (
        problemDetails.status === 400 &&
        problemDetails.title === 'InvalidState'
      ) {
        setError('root', {
          type: 'manual',
          message: 'auction_changed',
        })
      } else {
        setError('root', {
          type: 'manual',
          message:
            problemDetails.detail ?? 'Failed to place bid. Please try again.',
        })
      }
    }
  }

  return (
    <div>
      {isUserCurrentBidder && (
        <div className="mb-3 rounded-md border border-blue-200 bg-blue-50 px-3 py-2">
          <div className="flex items-center">
            <svg
              className="mr-2 h-5 w-5 text-blue-500"
              fill="currentColor"
              viewBox="0 0 20 20"
              xmlns="http://www.w3.org/2000/svg"
            >
              <path
                fillRule="evenodd"
                d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
                clipRule="evenodd"
              ></path>
            </svg>
            <span className="font-medium text-blue-800">
              You&apos;re the highest bidder!
            </span>
          </div>
          <p className="mt-1 pl-7 text-sm text-blue-700">
            Placing another bid will increase your maximum bid.
          </p>
        </div>
      )}

      {isFetching && (
        <div className="mb-2 flex items-center text-sm font-medium text-blue-600">
          <svg
            className="mr-2 -ml-1 h-4 w-4 animate-spin text-blue-600"
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
          >
            <circle
              className="opacity-25"
              cx="12"
              cy="12"
              r="10"
              stroke="currentColor"
              strokeWidth="4"
            ></circle>
            <path
              className="opacity-75"
              fill="currentColor"
              d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
            ></path>
          </svg>
          Refreshing auction data...
        </div>
      )}

      <div className="mt-3">
        <form
          onSubmit={(e) => {
            void (async () => {
              await handleSubmit(onSubmitBid)(e)
            })()
          }}
          className="flex items-center gap-2"
        >
          <div className="relative">
            <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3">
              <span className="text-gray-500 sm:text-sm">$</span>
            </div>
            <input
              type="number"
              step="0.01"
              min={minimumBid}
              disabled={isFetching || isPlacingBid}
              {...register('amount', {
                required: 'Bid amount is required',
                valueAsNumber: true,
                min: {
                  value: minimumBid,
                  message: `Bid must be at least $${minimumBid.toFixed(2)}`,
                },
              })}
              className="block w-full rounded-md border-0 bg-white py-2 pr-3 pl-7 text-gray-900 ring-1 ring-gray-300 ring-inset placeholder:text-gray-400 focus:ring-2 focus:ring-indigo-600 focus:ring-inset disabled:cursor-not-allowed disabled:bg-gray-100 disabled:text-gray-500 sm:text-sm sm:leading-6"
              placeholder="0.00"
              aria-describedby="bid-amount"
            />
          </div>
          <button
            type="submit"
            disabled={isFetching || isPlacingBid}
            className="rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-lime-300 hover:text-indigo-700 focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 focus:outline-none disabled:cursor-not-allowed disabled:opacity-50"
          >
            {isPlacingBid ? 'Bidding...' : 'Place Bid'}
          </button>
        </form>

        {(errors.amount ?? errors.root) && (
          <div className="mt-2">
            {errors.root?.message === 'auction_changed' ? (
              <div className="rounded-md border border-yellow-300 bg-yellow-50 px-3 py-2">
                <div className="flex items-center">
                  <svg
                    className="mr-2 h-5 w-5 text-yellow-600"
                    fill="currentColor"
                    viewBox="0 0 20 20"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path
                      fillRule="evenodd"
                      d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z"
                      clipRule="evenodd"
                    ></path>
                  </svg>
                  <span className="font-medium text-yellow-800">
                    Bid not accepted
                  </span>
                </div>
                <p className="mt-1 pl-7 text-sm text-yellow-700">
                  Auction details have changed, please try again.
                </p>
              </div>
            ) : (
              <p className="text-sm text-red-600">
                {errors.amount?.message ?? errors.root?.message}
              </p>
            )}
          </div>
        )}

        <p className="mt-2 text-xs text-gray-500">
          Minimum bid: ${minimumBid.toFixed(2)}
        </p>
      </div>
    </div>
  )
}
