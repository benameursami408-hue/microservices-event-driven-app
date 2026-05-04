import { describe, expect, it } from 'vitest'
import { priorityLabel, reclamationStatusLabel, roleLabel, slaStatusLabel } from './enums'

describe('enum display helpers', () => {
  it('renders role labels for numeric and string values', () => {
    expect(roleLabel(0)).toBe('Client')
    expect(roleLabel('SAV')).toBe('SAV')
    expect(roleLabel('3')).toBe('Technique')
  })

  it('renders reclamation statuses in French for API values', () => {
    expect(reclamationStatusLabel(0)).toBe('Nouveau')
    expect(reclamationStatusLabel('IN_PROGRESS')).toBe('En cours')
    expect(reclamationStatusLabel('CLOSED')).toBe('Ferme')
  })

  it('renders priority and SLA labels used by demo pages', () => {
    expect(priorityLabel(3)).toBe('Urgente')
    expect(slaStatusLabel('NearBreach')).toBe('Attention SLA')
    expect(slaStatusLabel(2)).toBe('SLA depasse')
  })
})
