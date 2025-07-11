import * as testingLibraryUserEvent from '@testing-library/user-event'
import { render, RenderOptions, screen } from '@testing-library/react'
import { InitialEntry, MemoryRouter, Path, useLocation } from 'react-router'
import { AppStore, RootState, setupStore } from '../stores/store.ts'
import { JSX, PropsWithChildren, ReactElement } from 'react'
import { Provider } from 'react-redux'

export const userEvent = testingLibraryUserEvent.default.setup({ delay: null })

interface Location<State> extends Path {
  state: State
  key: string
}

interface RenderWithProvidersProps extends Omit<RenderOptions, 'queries'> {
  preloadedState?: Partial<RootState>
  store?: AppStore
}

export function renderWithProviders(
  ui: ReactElement,
  {
    preloadedState = {},
    store = setupStore(preloadedState),
    ...renderOptions
  }: RenderWithProvidersProps = {}
) {
  function Wrapper({ children }: PropsWithChildren<object>): JSX.Element {
    return <Provider store={store}>{children}</Provider>
  }
  return { store, ...render(ui, { wrapper: Wrapper, ...renderOptions }) }
}

interface RenderWithProvidersAndRouterProps extends RenderWithProvidersProps {
  initialEntries?: InitialEntry[]
}

export function renderWithProvidersAndRouter(
  ui: ReactElement,
  {
    preloadedState = {},
    store = setupStore(preloadedState),
    initialEntries = undefined,
    ...renderOptions
  }: RenderWithProvidersAndRouterProps = {}
) {
  function Wrapper({ children }: PropsWithChildren<object>): JSX.Element {
    return (
      <Provider store={store}>
        <MemoryRouter initialEntries={initialEntries}>
          {children}
          <CurrentLocation />
        </MemoryRouter>
      </Provider>
    )
  }

  const getCurrentLocation = <State = unknown,>() => {
    const attribute = screen
      .getByTestId('__current-location')
      .getAttribute('data-location')
    if (!attribute) return null
    return JSON.parse(attribute) as Location<State>
  }

  return {
    store,
    getCurrentLocation,
    ...render(ui, { wrapper: Wrapper, ...renderOptions }),
  }
}

// eslint-disable-next-line react-refresh/only-export-components
const CurrentLocation = () => {
  const location = useLocation()

  return (
    <div
      data-testid={'__current-location'}
      data-location={JSON.stringify(location)}
    ></div>
  )
}
