import { Link } from 'react-router'
import { useGetAuctionsQuery } from '../auctionsApiSlice'
import Loading from '../../../components/Loading'
import ErrorMessage from '../../../components/ErrorMessage'
import { SummaryAuctionResponse } from '../../../api/ApiGenerated.ts'

function formatTimeRemaining(endDate: Date): string {
  const now = new Date()
  const end = new Date(endDate)
  const diff = end.getTime() - now.getTime()

  if (diff <= 0) return 'Ended'

  const days = Math.floor(diff / (1000 * 60 * 60 * 24))
  const hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60))
  const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60))

  if (days > 0) return `${days.toString()}d ${hours.toString()}h`
  if (hours > 0) return `${hours.toString()}h ${minutes.toString()}m`
  return `${minutes.toString()}m`
}

function truncateDescription(description: string, maxLength = 120): string {
  if (description.length <= maxLength) return description
  return description.substring(0, maxLength).trim() + '...'
}

interface AuctionCardProps {
  auction: SummaryAuctionResponse
}

function AuctionCard({ auction }: AuctionCardProps) {
  return (
    <Link
      to={`/auctions/${auction.id}`}
      className="group block overflow-hidden rounded-lg border border-gray-200 bg-white shadow-sm transition-shadow hover:shadow-md"
    >
      <div className="aspect-w-16 aspect-h-9 overflow-hidden bg-gray-100">
        <img
          src={auction.imageUrl}
          alt={auction.name}
          className="h-48 w-full object-cover transition-transform group-hover:scale-105"
        />
      </div>

      <div className="p-4">
        <h3 className="text-lg font-semibold text-gray-900 transition-colors group-hover:text-indigo-600">
          {auction.name}
        </h3>

        <p className="mt-2 line-clamp-3 text-sm text-gray-600">
          {truncateDescription(auction.description)}
        </p>

        <div className="mt-4 flex items-center justify-between">
          <div>
            <p className="text-2xl font-bold text-green-600">
              ${auction.currentPrice.toFixed(2)}
            </p>
            <p className="text-sm text-gray-500">
              {auction.numberOfBids}{' '}
              {auction.numberOfBids === 1 ? 'bid' : 'bids'}
            </p>
          </div>

          <div className="text-right">
            <p className="text-sm font-medium text-gray-900">
              {formatTimeRemaining(auction.endDate)}
            </p>
            <p className="text-xs text-gray-500">remaining</p>
          </div>
        </div>

        <div className="mt-3 border-t border-gray-100 pt-3">
          <p className="text-sm text-gray-600">
            Sold by{' '}
            <span className="font-medium">{auction.sellerDisplayName}</span>
          </p>
        </div>
      </div>
    </Link>
  )
}

export default function CurrentAuctions() {
  const { data: response, isLoading, error } = useGetAuctionsQuery()

  if (isLoading) {
    return <Loading />
  }

  if (error) {
    return (
      <ErrorMessage message="Unable to load auctions. Please try again later." />
    )
  }

  if (!response || response.auctions.length === 0) {
    return (
      <div className="py-12 text-center">
        <svg
          className="mx-auto h-12 w-12 text-gray-400"
          stroke="currentColor"
          fill="none"
          viewBox="0 0 48 48"
        >
          <path
            d="M34 40h10v-4a6 6 0 00-10.712-3.714M34 40H14m20 0v-4a9.971 9.971 0 00-.712-3.714M14 40H4v-4a6 6 0 0110.713-3.714M14 40v-4c0-1.313.253-2.566.713-3.714m0 0A10.003 10.003 0 0124 26c4.21 0 7.813 2.602 9.288 6.286M30 14a6 6 0 11-12 0 6 6 0 0112 0zm12 6a4 4 0 11-8 0 4 4 0 018 0zm-28 0a4 4 0 11-8 0 4 4 0 018 0z"
            strokeWidth={2}
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
        <p className="mt-1 text-sm text-gray-500">
          There are no active auctions :(
        </p>
      </div>
    )
  }

  return (
    <div className="mx-auto max-w-7xl px-4 py-8">
      <div className="mb-8 flex items-center justify-between">
        <h1 className="text-3xl font-bold text-gray-900">Current Auctions</h1>
      </div>

      <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
        {response.auctions.map((auction) => (
          <AuctionCard key={auction.id} auction={auction} />
        ))}
      </div>
    </div>
  )
}
