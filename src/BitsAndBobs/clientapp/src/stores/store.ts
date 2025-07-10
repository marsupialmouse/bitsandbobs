import { combineReducers, configureStore } from '@reduxjs/toolkit'
import userContextReducer from '../features/usercontext/userContextSlice'
import { api } from '../api/apiSlice'

const rootReducer = combineReducers({
  userContext: userContextReducer,
  [api.reducerPath]: api.reducer,
})

export function setupStore(preloadedState?: Partial<RootState>) {
  return configureStore({
    reducer: rootReducer,
    preloadedState,
    middleware: (getDefaultMiddleware) =>
      getDefaultMiddleware().concat(api.middleware),
  })
}

export type RootState = ReturnType<typeof rootReducer>
export type AppStore = ReturnType<typeof setupStore>
export type AppDispatch = AppStore['dispatch']
