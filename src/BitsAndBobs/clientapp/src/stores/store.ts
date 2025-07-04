import { combineReducers, configureStore } from '@reduxjs/toolkit'
import identityReducer from '../features/identity/identitySlice'

const rootReducer = combineReducers({
  identity: identityReducer,
})

export function setupStore(preloadedState?: Partial<RootState>) {
  return configureStore({
    reducer: rootReducer,
    preloadedState,
  })
}

export type RootState = ReturnType<typeof rootReducer>
export type AppStore = ReturnType<typeof setupStore>
export type AppDispatch = AppStore['dispatch']
