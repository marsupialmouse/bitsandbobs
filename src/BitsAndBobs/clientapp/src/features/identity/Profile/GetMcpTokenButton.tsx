import { useState } from 'react'
import { useGetJwtTokenMutation } from '../identityApiSlice'

export default function GetMcpTokenButton() {
  const [getJwtToken, { isLoading }] = useGetJwtTokenMutation()
  const [tokenRetrieved, setTokenRetrieved] = useState(false)

  const handleGetMCPToken = async () => {
    try {
      const result = await getJwtToken().unwrap()
      if (result.token) {
        await navigator.clipboard.writeText(result.token)
        setTokenRetrieved(true)
        // Reset the state after 5 seconds
        setTimeout(() => {
          setTokenRetrieved(false)
        }, 5000)
      }
    } catch (error) {
      console.error('Failed to get MCP token:', error)
    }
  }

  return (
    <button
      type="button"
      onClick={() => {
        void handleGetMCPToken()
      }}
      disabled={isLoading}
      className={`flex w-full justify-center rounded-md border px-4 py-2 text-sm font-medium shadow-sm focus:ring-2 focus:ring-offset-2 focus:outline-none disabled:opacity-50 ${
        tokenRetrieved
          ? 'border-green-300 bg-green-50 text-green-700 hover:bg-green-100 focus:ring-green-500'
          : 'border-gray-300 bg-white text-gray-700 hover:bg-gray-50 focus:ring-indigo-500'
      }`}
    >
      {isLoading
        ? 'Getting token...'
        : tokenRetrieved
          ? '✓ Token copied to clipboard'
          : 'Get MCP Token'}
    </button>
  )
}
