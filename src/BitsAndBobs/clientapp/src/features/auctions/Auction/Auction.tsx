import {
  useCancelAuctionMutation,
  useGetAuctionQuery,
} from '../auctionsApiSlice.ts'
import Loading from '../../../components/Loading.tsx'
import ErrorMessage from '../../../components/ErrorMessage.tsx'
import BidList from './BidList.tsx'
import AddBid from './AddBid.tsx'
import { Link, useParams } from 'react-router'
import { useSelector } from 'react-redux'
import { selectIsAuthenticated } from '../../usercontext/userContextSlice.ts'
import AuctionTimeRemaining from '../AuctionTimeRemaining/AuctionTimeRemaining.tsx'
import useLocalDate from '../../useLocalDate.ts'

export default function Auction() {
  const { id } = useParams() as { id: string }
  const { formatDateWithTime } = useLocalDate()
  const {
    data: auction,
    isLoading,
    error,
    isFetching,
  } = useGetAuctionQuery(id, {
    pollingInterval: 30000,
    skipPollingIfUnfocused: true,
  })
  const [cancelAuction, { isLoading: isCancelling }] =
    useCancelAuctionMutation()
  const isAuthenticated = useSelector(selectIsAuthenticated)

  const handleCancelAuction = async () => {
    if (
      auction &&
      window.confirm('Are you sure you want to cancel this auction?')
    ) {
      try {
        await cancelAuction(auction.id).unwrap()
      } catch (error) {
        console.error('Failed to cancel auction:', error)
      }
    }
  }

  if (isLoading) {
    return <Loading />
  }

  // Check if error is a 404 (Not Found) error
  if (error && 'status' in error && error.status === 404) {
    return (
      <div className="py-12 text-center">
        <h2 className="text-2xl font-bold text-gray-900">Auction not found</h2>
        <p className="mt-2 text-sm text-gray-500">
          The auction you&apos;re looking for doesn&apos;t exist.
        </p>
      </div>
    )
  }

  if (error || !auction) {
    return (
      <ErrorMessage message="Unable to load auction details. Please try again later." />
    )
  }

  return (
    <div className="mx-auto max-w-4xl px-4 py-8">
      <div className="overflow-hidden rounded-lg border border-gray-200 bg-white shadow-md">
        {/* Image Section */}
        <div className="aspect-w-16 aspect-h-9 overflow-hidden bg-gray-100">
          <img
            src={auction.imageHref}
            alt={auction.name}
            className="h-64 w-full object-cover sm:h-80 lg:h-96"
          />
        </div>

        {/* Content Section */}
        <div className="p-6 sm:p-8">
          <div className="mb-6">
            <div className="flex items-start justify-between">
              <h1 className="text-3xl font-bold text-gray-900 sm:text-4xl">
                {auction.name}
              </h1>
              {auction.isCancelled && (
                <span className="rounded-full bg-red-100 px-3 py-1 text-sm font-medium text-red-800">
                  Cancelled
                </span>
              )}
              {auction.isClosed && !auction.isCancelled && (
                <span className="rounded-full bg-gray-100 px-3 py-1 text-sm font-medium text-gray-800">
                  Ended
                </span>
              )}
              {auction.isOpen && (
                <span className="rounded-full bg-green-100 px-3 py-1 text-sm font-medium text-green-800">
                  Active
                </span>
              )}
            </div>
            <p className="mt-4 text-lg leading-relaxed text-gray-600">
              {auction.description}
            </p>
          </div>

          {/* Pricing and Time Info */}
          <div className="mb-6 grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div className="rounded-lg border border-gray-200 bg-gray-50 p-4">
              <h3 className="text-sm font-medium tracking-wide text-gray-500 uppercase">
                Current Price
              </h3>
              <p className="mt-1 text-3xl font-bold text-green-600">
                ${auction.currentPrice.toFixed(2)}
              </p>
              <p className="mt-1 text-sm text-gray-600">
                {auction.numberOfBids}{' '}
                {auction.numberOfBids === 1 ? 'bid' : 'bids'}
              </p>
              {auction.currentBidderDisplayName && (
                <p className="mt-1 text-sm text-gray-600">
                  High bidder: {auction.currentBidderDisplayName}
                </p>
              )}
            </div>

            {auction.isOpen && (
              <div className="rounded-lg border border-gray-200 bg-gray-50 p-4">
                <h3 className="text-sm font-medium tracking-wide text-gray-500 uppercase">
                  Time Remaining
                </h3>
                <p className="mt-1 text-3xl font-bold text-gray-900">
                  <AuctionTimeRemaining endDate={auction.endDate} />
                </p>
                <p className="mt-1 text-sm text-gray-600">
                  Ends: {formatDateWithTime(auction.endDate)}
                </p>
              </div>
            )}

            {!auction.isOpen && (
              <div className="rounded-lg border border-gray-200 bg-gray-50 p-4">
                <h3 className="text-sm font-medium tracking-wide text-gray-500 uppercase">
                  {auction.isCancelled ? 'Cancelled' : 'Ended'}
                </h3>
                <p className="mt-1 text-sm text-gray-600">
                  {auction.isCancelled && auction.cancelledDate
                    ? `Cancelled: ${formatDateWithTime(auction.cancelledDate)}`
                    : `Ended: ${formatDateWithTime(auction.endDate)}`}
                </p>
              </div>
            )}
          </div>

          <div className="mb-6 grid grid-cols-1 gap-4 sm:grid-cols-2">
            {/* Bidding Section */}
            <div className="rounded-lg border border-gray-200 bg-gray-50 p-4">
              <h3 className="text-sm font-medium tracking-wide text-gray-500 uppercase">
                Ready to bid?
              </h3>
              <div className="mt-1">
                {!auction.isOpen && (
                  <p className="mb-3 text-sm text-gray-600">
                    too late - it&apos;s over!
                  </p>
                )}
                {auction.isOpen && isAuthenticated && auction.isUserSeller && (
                  <p className="mb-3 text-sm text-gray-600">
                    You can&apos;t bid on your own auction, you duffer
                  </p>
                )}
                {auction.isOpen && isAuthenticated && !auction.isUserSeller && (
                  <AddBid
                    auctionId={auction.id}
                    minimumBid={auction.minimumBid}
                    isUserCurrentBidder={auction.isUserCurrentBidder}
                    isFetching={isFetching}
                  />
                )}
                {auction.isOpen && !isAuthenticated && (
                  <div>
                    <p className="mt-2 mb-3 text-sm text-gray-600">
                      Sign in or create an account to place a bid
                    </p>
                    <div className="flex flex-col gap-2 sm:flex-row">
                      <Link
                        to="/login"
                        className="flex-1 rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-lime-300 hover:text-indigo-700 focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 focus:outline-none"
                      >
                        Sign In
                      </Link>
                      <Link
                        to="/register"
                        className="flex-1 rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-lime-300 hover:text-indigo-700 focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 focus:outline-none"
                      >
                        Sign Up
                      </Link>
                    </div>
                  </div>
                )}
              </div>
            </div>

            <div className="rounded-lg border border-gray-200 bg-gray-50 p-4">
              <h3 className="text-sm font-medium tracking-wide text-gray-500 uppercase">
                Seller Information
              </h3>
              <div className="mt-2 flex items-center">
                <div className="flex h-8 w-8 items-center justify-center rounded-full bg-indigo-600">
                  <span className="text-sm font-medium text-white">
                    {auction.sellerDisplayName.charAt(0)}
                  </span>
                </div>
                <p className="ml-3 text-lg font-medium text-gray-900">
                  {auction.sellerDisplayName}
                </p>
              </div>
            </div>
          </div>

          {/* Bid History */}
          {auction.bids && (
            <div className="mb-6">
              <BidList bids={[...auction.bids].reverse()} />
            </div>
          )}

          {/* Action Buttons */}
          <div className="flex flex-col gap-3 sm:flex-row">
            {auction.isOpen && auction.isUserSeller && (
              <button
                onClick={() => {
                  void (async () => {
                    await handleCancelAuction()
                  })()
                }}
                disabled={isCancelling}
                className="flex-1 rounded-md bg-red-600 px-6 py-3 text-base font-medium text-white transition-colors hover:bg-red-700 focus:ring-2 focus:ring-red-500 focus:ring-offset-2 focus:outline-none disabled:cursor-not-allowed disabled:opacity-50"
              >
                {isCancelling ? 'Cancelling...' : 'Cancel Auction'}
              </button>
            )}
            {!auction.isOpen && auction.isUserSeller && (
              <Link
                role="button"
                to={`/auction/${auction.id}/relist`}
                className="flex-1 rounded-md bg-indigo-600 px-6 py-3 text-center text-base font-medium text-white transition-colors hover:bg-indigo-700 focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 focus:outline-none disabled:cursor-not-allowed disabled:opacity-50"
              >
                Relist Item
              </Link>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
