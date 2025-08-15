import { createSlice } from '@reduxjs/toolkit'
import { RootState } from '../../stores/store'
import { identityApi } from '../identity/identityApiSlice.ts'
import { userContextApi } from './userContextApiSlice.ts'

export interface UserContextState {
  emailAddress?: string
  isAuthenticated: boolean
  localTimeOffset: number
}

export const initialUserContextState: UserContextState = {
  isAuthenticated: false,
  localTimeOffset: 0,
}

export const userContextSlice = createSlice({
  name: 'userContext',
  initialState: initialUserContextState,
  reducers: {},
  extraReducers: (builder) => {
    builder.addMatcher(
      userContextApi.endpoints.getUserContext.matchFulfilled,
      (state, { payload }) => {
        const l =
          'localDate' in payload ? new Date(payload.localDate) : new Date()
        state.isAuthenticated = payload.isAuthenticated
        state.emailAddress = payload.emailAddress
        state.localTimeOffset = new Date().getTime() - l.getTime()
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

export const selectLocalTimeOffset = (state: RootState) =>
  state.userContext.localTimeOffset

export default userContextSlice.reducer
