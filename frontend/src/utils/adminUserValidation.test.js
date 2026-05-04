import { describe, expect, it } from 'vitest'
import { validateAdminUserForm } from './adminUserValidation'

describe('validateAdminUserForm', () => {
  const validForm = {
    firstName: '  Ali ',
    lastName: '  Ben Salah ',
    email: '  ALI@example.com ',
    phoneNumber: '+216 22 000 000',
    address: ' Tunis ',
    role: 'SAV',
    isActive: 'true',
    password: 'SavTest!123',
  }

  it('normalizes valid create payloads before sending them to the API', () => {
    const result = validateAdminUserForm(validForm)

    expect(result.ok).toBe(true)
    expect(result.value).toMatchObject({
      firstName: 'Ali',
      lastName: 'Ben Salah',
      email: 'ALI@example.com',
      phoneNumber: '+216 22 000 000',
      address: 'Tunis',
      role: 'SAV',
      isActive: true,
      password: 'SavTest!123',
    })
  })

  it('blocks short passwords on create', () => {
    const result = validateAdminUserForm({ ...validForm, password: '123' })

    expect(result.ok).toBe(false)
    expect(result.message).toContain('au moins 8 caracteres')
  })

  it('allows updates without changing the password', () => {
    const result = validateAdminUserForm({ ...validForm, password: '' }, { editing: true })

    expect(result.ok).toBe(true)
    expect(result.value.password).toBe('')
  })

  it('rejects unsupported roles before sending the request', () => {
    const result = validateAdminUserForm({ ...validForm, role: 'MANAGER' })

    expect(result.ok).toBe(false)
    expect(result.message).toContain('role')
  })
})
