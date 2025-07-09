import { api } from '../../api/apiSlice'
import { createSelector } from '@reduxjs/toolkit'
import { UserContextResponse } from '../../api/ApiGenerated.ts'

const userContextApi = api
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

const selectUserContext = userContextApi.endpoints.getUserContext.select()

export const selectIsAuthenticated = createSelector(
  selectUserContext,
  (contextResult) => contextResult.data?.isAuthenticated ?? false
)

export const selectCurrentEmailAddress = createSelector(
  selectUserContext,
  (contextResult) => contextResult.data?.emailAddress
)

export const selectCurrentUsername = createSelector(
  selectUserContext,
  (contextResult) => contextResult.data?.username
)
