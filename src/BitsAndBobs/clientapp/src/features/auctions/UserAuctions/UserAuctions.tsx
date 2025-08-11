import { useState } from 'react'
import { useGetSellerAuctionsQuery } from '../auctionsApiSlice.ts'
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
        {activeTab === 'won' && <ComingSoon text="Won auctions" />}
        {activeTab === 'participating' && (
          <ComingSoon text="Auctions you're participating in" />
        )}
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

function ComingSoon({ text }: { text: string }) {
  return (
    <div className="rounded-lg bg-gray-50 py-16 text-center">
      <h3 className="mb-2 text-lg font-medium text-gray-900">Coming Soon</h3>
      <p className="text-gray-500">{text} will be available soon!</p>
    </div>
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
    />
  )
}
