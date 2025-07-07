import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react'

export const api = createApi({
  reducerPath: 'api',
  baseQuery: fetchBaseQuery({
    baseUrl: (import.meta.env.VITE_API_BASE_URL as string) || '/api',
    prepareHeaders: (headers) => {
      const xsrfToken = getCookie('XSRF-TOKEN')
      if (xsrfToken) {
        headers.set('X-XSRF-TOKEN', xsrfToken)
      }
      return headers
    },
    credentials: 'include',
  }),

  endpoints: () => ({}),
})

function getCookie(name: string): string | null {
  const value = `; ${document.cookie}`
  const parts = value.split(`; ${name}=`)
  if (parts.length === 2) return parts.pop()?.split(';').shift() ?? null
  return null
}
