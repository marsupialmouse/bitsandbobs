import { api } from '../../api/apiSlice'
import { UserContextResponse } from '../../api/ApiGenerated.ts'

export const userContextApi = api
  .enhanceEndpoints({ addTagTypes: ['UserContext'] })
  .injectEndpoints({
    endpoints: (builder) => ({
      getUserContext: builder.query<UserContextResponse, void>({
        query: () => '/usercontext',
        providesTags: ['UserContext'],
      }),
    }),
    overrideExisting: false,
  })

export const { useGetUserContextQuery } = userContextApi
