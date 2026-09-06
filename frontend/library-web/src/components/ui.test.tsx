import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { StatusPill } from './ui'

describe('StatusPill', () => {
  it('renders the status label', () => {
    render(<StatusPill status="Suspended" />)
    expect(screen.getByText('Suspended')).toBeInTheDocument()
  })
})
