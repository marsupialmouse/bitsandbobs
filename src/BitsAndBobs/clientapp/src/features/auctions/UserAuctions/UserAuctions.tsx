import { useState } from 'react'
import {
  useGetParticipantAuctionsQuery,
  useGetSellerAuctionsQuery,
  useGetWonAuctionsQuery,
} from '../auctionsApiSlice.ts'
import AuctionList from './AuctionList.tsx'

type TabType = 'seller' | 'won' | 'participating'

export default function UserAuctions() {
  const [activeTab, setActiveTab] = useState<TabType>('seller')

  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="mb-6 text-3xl font-bold text-gray-900">My Auctions</h1>

      {/* Tabs */}
      <div className="mb-6 border-b border-gray-200">
        <nav className="-mb-px flex space-x-8" aria-label="Tabs">
          <TabButton
            label="Selling"
            isActive={activeTab === 'seller'}
            onClick={() => {
              setActiveTab('seller')
            }}
          />
          <TabButton
            label="Won"
            isActive={activeTab === 'won'}
            onClick={() => {
              setActiveTab('won')
            }}
          />
          <TabButton
            label="Participating"
            isActive={activeTab === 'participating'}
            onClick={() => {
              setActiveTab('participating')
            }}
          />
        </nav>
      </div>

      {/* Tab Content */}
      <div className="mt-4">
        {activeTab === 'seller' && <SellerAuctions />}
        {activeTab === 'won' && <WonAuctions />}
        {activeTab === 'participating' && <ParticipantAuctions />}
      </div>
    </div>
  )
}

function TabButton({
  label,
  isActive,
  onClick,
}: {
  label: string
  isActive: boolean
  onClick: () => void
}) {
  return (
    <button
      className={`border-b-2 px-1 py-4 text-sm font-medium whitespace-nowrap ${
        isActive
          ? 'border-indigo-500 text-indigo-600'
          : 'border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700'
      }`}
      onClick={onClick}
      aria-current={isActive ? 'page' : undefined}
    >
      {label}
    </button>
  )
}

function ParticipantAuctions() {
  const { data, isLoading, isError } = useGetParticipantAuctionsQuery()

  return (
    <AuctionList
      auctions={data?.auctions}
      isLoading={isLoading}
      isError={isError}
      emptyMessage="You haven't bid on any auctions yet."
      errorMessage="Unable to load your auctions. Please try again later."
      showCreateButton={false}
      tabType="participating"
    />
  )
}

function SellerAuctions() {
  const { data, isLoading, isError } = useGetSellerAuctionsQuery()

  return (
    <AuctionList
      auctions={data?.auctions}
      isLoading={isLoading}
      isError={isError}
      emptyMessage="You haven't created any auctions yet."
      errorMessage="Unable to load your auctions. Please try again later."
      showCreateButton={true}
      tabType="seller"
    />
  )
}

function WonAuctions() {
  const { data, isLoading, isError } = useGetWonAuctionsQuery()

  return (
    <AuctionList
      auctions={data?.auctions}
      isLoading={isLoading}
      isError={isError}
      emptyMessage="You haven't won any auctions yet."
      errorMessage="Unable to load your auctions. Please try again later."
      showCreateButton={false}
      tabType="won"
    />
  )
}
