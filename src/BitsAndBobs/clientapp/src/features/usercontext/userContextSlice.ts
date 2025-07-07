import { createSlice, PayloadAction } from '@reduxjs/toolkit'
import { RootState } from '../../stores/store'

export interface UserContextState {
  emailAddress: string | undefined
  username: string | undefined
  isAuthenticated: boolean
}

export const initialUserContextState: UserContextState = {
  emailAddress: undefined,
  username: undefined,
  isAuthenticated: false,
}

export const userContextSlice = createSlice({
  name: 'userContext',
  initialState: initialUserContextState,
  reducers: {
    signedIn(
      state,
      action: PayloadAction<{ emailAddress: string; username: string }>
    ) {
      state.emailAddress = action.payload.emailAddress
      state.username = action.payload.username
      state.isAuthenticated = true
    },
    signedOut(state) {
      state.emailAddress = undefined
      state.username = undefined
      state.isAuthenticated = false
    },
  },
})

export const { signedIn, signedOut } = userContextSlice.actions

export const selectIsAuthenticated = (state: RootState) =>
  state.identity.isAuthenticated
export const selectUsername = (state: RootState) => state.identity.username

export default userContextSlice.reducer
