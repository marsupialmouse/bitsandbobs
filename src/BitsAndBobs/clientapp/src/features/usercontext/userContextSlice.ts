import { createSlice } from '@reduxjs/toolkit'
import { RootState } from '../../stores/store'
import { identityApi } from '../identity/identityApiSlice.ts'
import { userContextApi } from './userContextApiSlice.ts'

export interface UserContextState {
  emailAddress?: string
  isAuthenticated: boolean
}

export const initialUserContextState: UserContextState = {
  isAuthenticated: false,
}

export const userContextSlice = createSlice({
  name: 'userContext',
  initialState: initialUserContextState,
  reducers: {},
  extraReducers: (builder) => {
    builder.addMatcher(
      userContextApi.endpoints.getUserContext.matchFulfilled,
      (state, { payload }) => {
        state.isAuthenticated = payload.isAuthenticated ?? false
        state.emailAddress = payload.emailAddress
      }
    )
    builder.addMatcher(
      identityApi.endpoints.login.matchFulfilled,
      (state, { payload }) => {
        state.isAuthenticated = true
        state.emailAddress = payload.emailAddress
      }
    )
    builder.addMatcher(identityApi.endpoints.logout.matchFulfilled, (state) => {
      state.isAuthenticated = false
      state.emailAddress = undefined
    })
    builder.addMatcher(
      identityApi.endpoints.confirmEmail.matchFulfilled,
      (state, { payload }) => {
        if (payload.changedEmail) state.emailAddress = payload.changedEmail
      }
    )
  },
})

export const selectIsAuthenticated = (state: RootState) =>
  state.userContext.isAuthenticated

export const selectCurrentEmailAddress = (state: RootState) =>
  state.userContext.emailAddress

export const selectCurrentUsername = (state: RootState) => {
  if (!state.userContext.emailAddress) return undefined
  return state.userContext.emailAddress.split('@')[0]
}

export default userContextSlice.reducer
