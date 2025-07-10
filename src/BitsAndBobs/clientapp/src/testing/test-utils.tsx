import * as testingLibraryUserEvent from '@testing-library/user-event'
import { render, RenderOptions } from '@testing-library/react'
import { InitialEntry, MemoryRouter } from 'react-router'
import { AppStore, RootState, setupStore } from '../stores/store.ts'
import { JSX, PropsWithChildren, ReactElement } from 'react'
import { Provider } from 'react-redux'

export const userEvent = testingLibraryUserEvent.default.setup({ delay: null })

/**
 * Renders a React element with a MemoryRouter for testing purposes.
 * @param ui - The React element to render.
 */
export function renderWithRouter(ui: React.ReactElement) {
  return render(<MemoryRouter>{ui}</MemoryRouter>)
}

interface ExtendedRenderOptions extends Omit<RenderOptions, 'queries'> {
  preloadedState?: Partial<RootState>
  store?: AppStore
  initialEntries?: InitialEntry[]
}

/**
 * Renders a component inside the Redux store provider and MemoryRouter
 * @param ui The component to render
 * @param param1 Any default state
 * @returns
 */
export function renderWithProvider(
  ui: ReactElement,
  {
    preloadedState = {},
    store = setupStore(preloadedState),
    ...renderOptions
  }: ExtendedRenderOptions = {}
) {
  function Wrapper({ children }: PropsWithChildren<object>): JSX.Element {
    return <Provider store={store}>{children}</Provider>
  }
  return { store, ...render(ui, { wrapper: Wrapper, ...renderOptions }) }
}

/**
 * Renders a component inside the Redux store provider and MemoryRouter
 * @param ui The component to render
 * @param param1 Any default state
 * @returns
 */
export function renderWithProviderAndRouter(
  ui: ReactElement,
  {
    preloadedState = {},
    store = setupStore(preloadedState),
    initialEntries = undefined,
    ...renderOptions
  }: ExtendedRenderOptions = {}
) {
  function Wrapper({ children }: PropsWithChildren<object>): JSX.Element {
    return (
      <Provider store={store}>
        <MemoryRouter initialEntries={initialEntries}>{children}</MemoryRouter>
      </Provider>
    )
  }
  return { store, ...render(ui, { wrapper: Wrapper, ...renderOptions }) }
}
