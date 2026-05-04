import { z } from 'zod'

export const ADMIN_USER_ROLE_VALUES = ['ADMIN', 'SAV', 'ST', 'CLIENT']

const phoneRegex = /^\+?[0-9][0-9\s().-]{5,19}$/

const baseSchema = z.object({
  firstName: z.string().trim().min(1, 'Le prenom est obligatoire.').max(50, 'Le prenom ne doit pas depasser 50 caracteres.'),
  lastName: z.string().trim().min(1, 'Le nom est obligatoire.').max(50, 'Le nom ne doit pas depasser 50 caracteres.'),
  email: z.string().trim().min(1, 'L email est obligatoire.').email('L email est invalide.').max(100, 'L email ne doit pas depasser 100 caracteres.'),
  phoneNumber: z
    .string()
    .trim()
    .min(1, 'Le telephone est obligatoire.')
    .max(20, 'Le telephone ne doit pas depasser 20 caracteres.')
    .regex(phoneRegex, 'Le telephone doit contenir 6 a 20 caracteres valides, par exemple +216 22 000 000.'),
  address: z.string().trim().max(250, 'L adresse ne doit pas depasser 250 caracteres.').optional().default(''),
  role: z.enum(ADMIN_USER_ROLE_VALUES, { message: 'Le role selectionne est invalide.' }),
})

function flattenIssues(error) {
  return error.issues.map((issue) => issue.message).join(' ')
}

function normalizeBoolean(value) {
  if (typeof value === 'boolean') return value
  return String(value).toLowerCase() === 'true'
}

export function validateAdminUserForm(form, { editing = false, fixedRole } = {}) {
  const role = fixedRole || form.role
  const parsed = baseSchema.safeParse({
    firstName: form.firstName,
    lastName: form.lastName,
    email: form.email,
    phoneNumber: form.phoneNumber,
    address: form.address,
    role,
  })

  if (!parsed.success) {
    return { ok: false, message: flattenIssues(parsed.error) }
  }

  const password = String(form.password ?? '').trim()
  if (!editing && password.length === 0) {
    return { ok: false, message: 'Le mot de passe est obligatoire pour un nouvel utilisateur.' }
  }

  if (password.length > 0 && password.length < 8) {
    return { ok: false, message: 'Le mot de passe doit contenir au moins 8 caracteres.' }
  }

  return {
    ok: true,
    value: {
      ...parsed.data,
      password,
      isActive: normalizeBoolean(form.isActive),
    },
  }
}
