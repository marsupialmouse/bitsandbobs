import * as testingLibraryUserEvent from '@testing-library/user-event'
import { render } from '@testing-library/react'
import { MemoryRouter } from 'react-router'

export const userEvent = testingLibraryUserEvent.default.setup({ delay: null })

/**
 * Renders a React element with a MemoryRouter for testing purposes.
 * @param ui - The React element to render.
 */
export function renderWithRouter(ui: React.ReactElement) {
  return render(<MemoryRouter>{ui}</MemoryRouter>)
}
