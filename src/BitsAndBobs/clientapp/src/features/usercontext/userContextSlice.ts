import { createSlice } from '@reduxjs/toolkit'
import { RootState } from '../../stores/store'
import { identityApi } from '../identity/identityApiSlice.ts'

export interface UserContextState {
  emailAddress: string | undefined
  isAuthenticated: boolean
}

export const initialUserContextState: UserContextState = {
  emailAddress: undefined,
  isAuthenticated: false,
}

export const userContextSlice = createSlice({
  name: 'userContext',
  initialState: initialUserContextState,
  reducers: {
    signedOut(state) {
      state.emailAddress = undefined
      state.isAuthenticated = false
    },
  },
  extraReducers: (builder) => {
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

export const { signedOut } = userContextSlice.actions
export const selectIsAuthenticated = (state: RootState) =>
  state.userContext.isAuthenticated

export const selectCurrentEmailAddress = (state: RootState) =>
  state.userContext.emailAddress

export const selectCurrentUsername = (state: RootState) => {
  if (!state.userContext.emailAddress) return undefined
  return state.userContext.emailAddress.split('@')[0]
}

export default userContextSlice.reducer
