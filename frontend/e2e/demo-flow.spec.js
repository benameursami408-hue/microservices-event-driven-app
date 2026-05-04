import { expect, test } from '@playwright/test'

const adminEmail = process.env.E2E_ADMIN_EMAIL || 'admin@sav.local'
const adminPassword = process.env.E2E_ADMIN_PASSWORD || 'Admin123!'

async function login(page, email = adminEmail, password = adminPassword) {
  await page.goto('/login')
  await page.getByLabel(/email/i).fill(email)
  await page.getByLabel(/mot de passe|password/i).fill(password)
  await page.getByRole('button', { name: /connexion|login/i }).click()
  await expect(page).toHaveURL(/\/app/)
}

test.describe('SAV demo smoke flow', () => {
  test('anonymous user is redirected to login before accessing the app', async ({ page }) => {
    await page.goto('/app/guide-test')
    await expect(page).toHaveURL(/login/)
  })

  test('admin can open the guided test page after login', async ({ page }) => {
    await login(page)
    await page.goto('/app/guide-test')

    await expect(page.getByRole('heading', { name: /guide de test/i })).toBeVisible()
    await expect(page.getByText(/preparer les comptes/i)).toBeVisible()
    await expect(page.getByText(/verifier les notifications/i)).toBeVisible()
  })

  test('admin users page validates a too-short password before create', async ({ page }) => {
    await login(page)
    await page.goto('/app/admin/users')

    await expect(page.getByText(/utilisateur|equipe|admin/i).first()).toBeVisible()
    await page.getByRole('button', { name: /ajouter|creer|nouveau/i }).click()

    await page.getByLabel(/prenom/i).fill('Test')
    await page.getByLabel(/nom/i).fill('User')
    await page.getByLabel(/email/i).fill(`test-${Date.now()}@example.com`)
    await page.getByLabel(/telephone/i).fill('+216 22 000 000')
    await page.getByLabel(/mot de passe|password/i).fill('123')
    await page.getByRole('button', { name: /enregistrer|creer|ajouter/i }).click()

    await expect(page.getByText(/au moins 8 caracteres/i)).toBeVisible()
  })
})
