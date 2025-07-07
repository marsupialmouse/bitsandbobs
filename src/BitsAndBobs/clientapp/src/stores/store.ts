import { combineReducers, configureStore } from '@reduxjs/toolkit'
import identityReducer from '../features/identity/identitySlice'
import { api } from '../api/apiSlice'

const rootReducer = combineReducers({
  identity: identityReducer,
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
