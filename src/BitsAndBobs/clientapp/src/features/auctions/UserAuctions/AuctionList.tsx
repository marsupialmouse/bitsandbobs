import { Link } from 'react-router'
import Loading from '../../../components/Loading.tsx'
import ErrorMessage from '../../../components/ErrorMessage.tsx'
import { UserAuction } from '../../../api/ApiGenerated.ts'
import AuctionTimeRemaining from '../AuctionTimeRemaining/AuctionTimeRemaining.tsx'
import useLocalDate from '../../useLocalDate.ts'

interface AuctionListProps {
  auctions?: UserAuction[]
  isLoading: boolean
  isError: boolean
  emptyMessage: string
  errorMessage: string
  showCreateButton?: boolean
  tabType?: 'seller' | 'won' | 'participating'
}

export default function AuctionList({
  auctions,
  isLoading,
  isError,
  emptyMessage,
  errorMessage,
  showCreateButton = false,
  tabType,
}: AuctionListProps) {
  const { formatDate } = useLocalDate()

  if (isLoading) {
    return <Loading />
  }

  if (isError) {
    return <ErrorMessage message={errorMessage} />
  }

  if (!auctions?.length) {
    return (
      <div className="rounded-lg bg-gray-50 py-16 text-center">
        <div className="mx-auto max-w-lg">
          <h3 className="mb-2 text-lg font-medium text-gray-900">
            {emptyMessage}
          </h3>
          {showCreateButton && (
            <Link
              to="/auctions/create"
              className="mt-4 inline-flex items-center rounded-md border border-transparent bg-indigo-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-indigo-700 focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 focus:outline-none"
            >
              Sell Something
            </Link>
          )}
        </div>
      </div>
    )
  }

  return (
    <div className="grid grid-cols-1 gap-3 lg:grid-cols-2">
      {auctions.map((auction) => (
        <div
          key={auction.id}
          className={`overflow-hidden rounded-lg border border-gray-200 bg-white shadow ${(auction.isClosed || auction.isCancelled) && tabType !== 'won' ? 'opacity-50' : ''}`}
        >
          <div className="flex p-3">
            <div className="mr-4 flex-shrink-0">
              <Link to={`/auction/${auction.id}`}>
                <img
                  src={auction.imageHref}
                  alt={auction.name}
                  className="h-24 w-24 rounded-lg object-cover"
                />
              </Link>
            </div>

            <div className="flex flex-1 flex-col justify-between">
              <div className="flex items-start justify-between">
                <div>
                  <h3 className="text-lg font-semibold text-gray-900">
                    <Link
                      to={`/auction/${auction.id}`}
                      className="hover:text-indigo-600"
                    >
                      {auction.name}
                    </Link>
                  </h3>

                  <div className="mt-1 flex flex-wrap gap-2">
                    {auction.isOpen && (
                      <span className="inline-flex items-center rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-800">
                        Active
                      </span>
                    )}
                    {auction.isClosed && !auction.isCancelled && (
                      <span className="inline-flex items-center rounded-full bg-gray-100 px-2 py-0.5 text-xs font-medium text-gray-800">
                        Ended
                      </span>
                    )}
                    {auction.isCancelled && (
                      <span className="inline-flex items-center rounded-full bg-red-100 px-2 py-0.5 text-xs font-medium text-red-800">
                        Cancelled
                      </span>
                    )}
                    {auction.isUserCurrentBidder && !auction.isClosed && (
                      <span className="inline-flex items-center rounded-full bg-blue-100 px-2 py-0.5 text-xs font-medium text-blue-800">
                        Leading Bid
                      </span>
                    )}
                    {auction.isUserCurrentBidder && auction.isClosed && (
                      <span className="inline-flex items-center rounded-full bg-blue-100 px-2 py-0.5 text-xs font-medium text-blue-800">
                        Winner!
                      </span>
                    )}
                    {auction.isClosed && auction.numberOfBids === 0 && (
                      <span className="inline-flex items-center rounded-full bg-yellow-100 px-2 py-0.5 text-xs font-medium text-yellow-800">
                        No Winner
                      </span>
                    )}
                  </div>
                </div>

                <div className="text-right">
                  <p className="text-lg font-bold text-green-600">
                    ${auction.currentPrice.toFixed(2)}
                  </p>
                  <p className="text-xs text-gray-500">
                    {auction.numberOfBids === 1
                      ? '1 bid'
                      : `${auction.numberOfBids.toString()} bids`}
                  </p>
                  {auction.userMaximumBid && (
                    <p className="text-xs text-gray-500">
                      Your max: ${auction.userMaximumBid.toFixed(2)}
                    </p>
                  )}
                </div>
              </div>

              <div className="mt-2 flex items-center justify-between">
                <div>
                  {auction.isOpen ? (
                    <div className="text-sm">
                      <span className="font-medium text-gray-700">
                        <AuctionTimeRemaining endDate={auction.endDate} />
                      </span>
                      <span className="ml-1 text-xs text-gray-500">
                        (ends {formatDate(auction.endDate)})
                      </span>
                    </div>
                  ) : auction.isCancelled ? (
                    <div className="text-sm text-gray-500">
                      Cancelled{' '}
                      {auction.cancelledDate &&
                        formatDate(auction.cancelledDate)}
                    </div>
                  ) : (
                    <div className="text-sm text-gray-500">
                      Ended {formatDate(auction.endDate)}
                    </div>
                  )}
                </div>

                <Link
                  to={`/auction/${auction.id}`}
                  className="inline-flex items-center rounded-md border border-transparent bg-indigo-600 px-3 py-1 text-xs font-medium text-white shadow-sm hover:bg-indigo-700 focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 focus:outline-none"
                >
                  View Details
                </Link>
              </div>
            </div>
          </div>
        </div>
      ))}
    </div>
  )
}
