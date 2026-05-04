import { describe, expect, it } from 'vitest'
import { DEMO_WORKFLOW_STEPS, getRoleNextSteps, normalizeRole } from './demoGuide'

describe('demo guide helpers', () => {
  it('contains the complete six-step demo workflow', () => {
    expect(DEMO_WORKFLOW_STEPS).toHaveLength(6)
    expect(DEMO_WORKFLOW_STEPS.map((step) => step.id)).toEqual([
      'users',
      'client-ticket',
      'ticket-list',
      'planning',
      'technician',
      'notifications',
    ])
  })

  it('normalizes numeric and text roles consistently', () => {
    expect(normalizeRole(0)).toBe('CLIENT')
    expect(normalizeRole('1')).toBe('SAV')
    expect(normalizeRole('admin')).toBe('ADMIN')
    expect(normalizeRole('ST')).toBe('ST')
  })

  it('returns a safe default journey for unknown roles', () => {
    const steps = getRoleNextSteps('UNKNOWN')

    expect(steps.title).toBe('Parcours conseille')
    expect(steps.actions.some((action) => action.route === '/app/guide-test')).toBe(true)
  })
})
