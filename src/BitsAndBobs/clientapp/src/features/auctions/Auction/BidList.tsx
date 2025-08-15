import { BidDetails } from '../../../api/ApiGenerated.ts'
import useLocalDate from '../../useLocalDate.ts'

interface BidListProps {
  bids: BidDetails[]
}

export default function BidList({ bids }: BidListProps) {
  const { formatDateWithTime } = useLocalDate()

  if (bids.length === 0) {
    return (
      <div className="rounded-lg border border-gray-200 bg-gray-50 p-4">
        <h3 className="text-sm font-medium tracking-wide text-gray-500 uppercase">
          Bid History
        </h3>
        <p className="mt-2 text-sm text-gray-600">
          No bids have been placed yet.
        </p>
      </div>
    )
  }

  return (
    <div className="rounded-lg border border-gray-200 bg-gray-50 p-4">
      <h3 className="mb-4 text-sm font-medium tracking-wide text-gray-500 uppercase">
        Bid History ({bids.length})
      </h3>
      <div className="max-h-60 space-y-3 overflow-y-auto">
        {bids.map((bid) => (
          <div
            key={bid.id}
            className={`rounded-lg p-3 ${
              bid.isUserBid
                ? 'border border-indigo-200 bg-indigo-50'
                : 'border border-gray-200 bg-white'
            }`}
          >
            <div className="flex items-center justify-between">
              <div className="flex items-center">
                <div
                  className={`flex h-6 w-6 items-center justify-center rounded-full text-xs font-medium ${
                    bid.isUserBid
                      ? 'bg-indigo-600 text-white'
                      : 'bg-gray-400 text-white'
                  }`}
                >
                  {bid.bidderDisplayName.charAt(0)}
                </div>
                <span className="ml-2 text-sm font-medium text-gray-900">
                  {bid.bidderDisplayName}
                  {bid.isUserBid && (
                    <span className="ml-2 text-xs font-medium text-indigo-600">
                      (You)
                    </span>
                  )}
                  {bid.isCurrentBid && (
                    <span className="ml-2 text-xs font-medium text-green-600">
                      (Current High Bid)
                    </span>
                  )}
                </span>
              </div>
              <div className="text-right">
                <p className="text-sm font-semibold text-gray-900">
                  ${bid.amount.toFixed(2)}
                </p>
                <p className="text-xs text-gray-500">
                  {formatDateWithTime(bid.bidDate)}
                </p>
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
