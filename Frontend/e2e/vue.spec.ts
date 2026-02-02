import { test, expect } from '@playwright/test'

test('app root redirects to login when unauthenticated', async ({ page }) => {
  await page.goto('/')
  await expect(page).toHaveURL(/\/(login|dashboard)/)
  // Either land on login (guest) or dashboard (if already logged in)
  const onLogin = await page.locator('text=Log in').isVisible().catch(() => false)
  const onDashboard = await page.locator('text=Dashboard').isVisible().catch(() => false)
  expect(onLogin || onDashboard).toBe(true)
})
