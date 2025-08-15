import { useParams } from 'react-router'
import { useEffect } from 'react'
import { useGetAuctionForRelistingMutation } from '../auctionsApiSlice.ts'
import Loading from '../../../components/Loading.tsx'
import ErrorMessage from '../../../components/ErrorMessage.tsx'
import CreateAuction from '../CreateAuction/CreateAuction.tsx'
import { GetAuctionForRelistingResponse } from '../../../api/ApiGenerated.ts'

export default function RelistAuction() {
  const { id } = useParams() as { id: string }
  const [getAuctionForRelisting, { data, isLoading, isError, error }] =
    useGetAuctionForRelistingMutation()

  useEffect(() => {
    void getAuctionForRelisting(id)
  }, [id, getAuctionForRelisting])

  if (!isError && (isLoading || !data)) {
    return <Loading />
  }

  if (isError && 'status' in error) {
    if (error.status == 404) {
      return (
        <div className="py-12 text-center">
          <h2 className="text-2xl font-bold text-gray-900">
            Auction not found
          </h2>
          <p className="mt-2 text-sm text-gray-500">
            The auction you&apos;re looking for doesn&apos;t exist.
          </p>
        </div>
      )
    }
    if (error.status == 400) {
      return (
        <div className="py-12 text-center">
          <h2 className="text-2xl font-bold text-gray-900">
            Unable to Relist Auction
          </h2>
          <p className="mt-2 text-sm text-gray-500">
            You can only relist your own auctions that are cancelled or
            complete.
          </p>
        </div>
      )
    }
  }

  if (isError) {
    return (
      <ErrorMessage message="Unable to load auction details. Please try again later." />
    )
  }

  return (
    <CreateAuction relistAuction={data as GetAuctionForRelistingResponse} />
  )
}
