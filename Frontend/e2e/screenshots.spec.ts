import { test } from '@playwright/test'
import { mkdirSync, existsSync } from 'node:fs'
import { join } from 'node:path'

const SCREENSHOTS_BASE = 'screenshots'

// Demo user for authenticated routes
const demoUser = {
  id: 1,
  name: 'Alice Johnson',
  email: 'alice@example.com',
  avatar: '👩‍💼',
}

// Device viewport sizes (matches useViewport breakpoints: tablet 768, desktop 1024)
const DEVICE_VIEWPORTS = {
  desktop: { width: 1280, height: 900 },
  tablet: { width: 834, height: 1194 },
  mobile: { width: 375, height: 812 },
} as const

function getDeviceFromProject(projectName: string): 'desktop' | 'tablet' | 'mobile' {
  if (projectName.includes('tablet')) return 'tablet'
  if (projectName.includes('mobile')) return 'mobile'
  return 'desktop'
}

function getScreenshotPath(device: string, feature: string, filename: string): string {
  const dir = join(SCREENSHOTS_BASE, device, feature)
  if (!existsSync(dir)) {
    mkdirSync(dir, { recursive: true })
  }
  return join(dir, filename)
}

async function setupAuth(page: import('@playwright/test').Page, user: typeof demoUser) {
  await page.goto('/login')
  await page.evaluate(
    (u) => {
      // Auth store persists as { user } under key 'auth'
      localStorage.setItem('auth', JSON.stringify({ user: u }))
      localStorage.setItem('theme', JSON.stringify({ theme: 'light' }))
      document.documentElement.classList.remove('dark')
      document.documentElement.classList.add('light')
    },
    user
  )
  await page.reload()
  await page.waitForLoadState('networkidle')
}

async function setupTheme(page: import('@playwright/test').Page, theme: 'light' | 'dark') {
  await page.evaluate(
    (t) => {
      localStorage.setItem('theme', t)
      document.documentElement.classList.remove('light', 'dark')
      document.documentElement.classList.add(t)
    },
    theme
  )
}

test.describe('Screenshots by device', () => {
  test.beforeEach(async ({ page }, testInfo) => {
    const device = getDeviceFromProject(testInfo.project.name)
    const viewport = DEVICE_VIEWPORTS[device]
    await page.setViewportSize(viewport)
  })

  test.describe('auth', () => {
    test('login - light', async ({ page }, testInfo) => {
      const device = getDeviceFromProject(testInfo.project.name)
      await page.goto('/login')
      await page.waitForLoadState('networkidle')
      await setupTheme(page, 'light')
      await page.waitForTimeout(300)
      await page.screenshot({
        path: getScreenshotPath(device, 'auth', 'login-light.png'),
        fullPage: device === 'mobile',
      })
    })

    test('login - dark', async ({ page }, testInfo) => {
      const device = getDeviceFromProject(testInfo.project.name)
      await page.goto('/login')
      await page.waitForLoadState('networkidle')
      await setupTheme(page, 'dark')
      await page.waitForTimeout(300)
      await page.screenshot({
        path: getScreenshotPath(device, 'auth', 'login-dark.png'),
        fullPage: device === 'mobile',
      })
    })

    test('signup - light', async ({ page }, testInfo) => {
      const device = getDeviceFromProject(testInfo.project.name)
      await page.goto('/signup')
      await page.waitForLoadState('networkidle')
      await setupTheme(page, 'light')
      await page.waitForTimeout(300)
      await page.screenshot({
        path: getScreenshotPath(device, 'auth', 'signup-light.png'),
        fullPage: device === 'mobile',
      })
    })

    test('signup - dark', async ({ page }, testInfo) => {
      const device = getDeviceFromProject(testInfo.project.name)
      await page.goto('/signup')
      await page.waitForLoadState('networkidle')
      await setupTheme(page, 'dark')
      await page.waitForTimeout(300)
      await page.screenshot({
        path: getScreenshotPath(device, 'auth', 'signup-dark.png'),
        fullPage: device === 'mobile',
      })
    })
  })

  test.describe('dashboard', () => {
    test.beforeEach(async ({ page }) => {
      await setupAuth(page, demoUser)
    })

    test('list view', async ({ page }, testInfo) => {
      const device = getDeviceFromProject(testInfo.project.name)
      await page.evaluate(() => {
        localStorage.setItem('taskViewMode', 'list')
      })
      await page.goto('/dashboard')
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(500)
      await page.screenshot({
        path: getScreenshotPath(device, 'dashboard', 'list.png'),
        fullPage: false,
      })
    })

    test('board view', async ({ page }, testInfo) => {
      const device = getDeviceFromProject(testInfo.project.name)
      await page.evaluate(() => {
        localStorage.setItem('taskViewMode', 'columns')
      })
      await page.goto('/dashboard')
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(500)
      await page.screenshot({
        path: getScreenshotPath(device, 'dashboard', 'board.png'),
        fullPage: false,
      })
    })

    test('analytics view', async ({ page }, testInfo) => {
      const device = getDeviceFromProject(testInfo.project.name)
      await page.evaluate(() => {
        localStorage.setItem('taskViewMode', 'analytics')
      })
      await page.goto('/dashboard')
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(500)
      await page.screenshot({
        path: getScreenshotPath(device, 'dashboard', 'analytics.png'),
        fullPage: device === 'mobile',
      })
    })

    test('task form modal', async ({ page }, testInfo) => {
      const device = getDeviceFromProject(testInfo.project.name)
      await page.goto('/dashboard')
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(500)
      const newTaskBtn = page.locator('button.btn-primary').filter({ hasText: /new/i }).first()
      await newTaskBtn.waitFor({ state: 'visible', timeout: 15000 })
      await newTaskBtn.click()
      await page.waitForTimeout(500)
      await page.screenshot({
        path: getScreenshotPath(device, 'dashboard', 'task-form-modal.png'),
        fullPage: false,
      })
    })
  })

  test.describe('my-tasks', () => {
    test.beforeEach(async ({ page }) => {
      await setupAuth(page, demoUser)
    })

    test('main view', async ({ page }, testInfo) => {
      const device = getDeviceFromProject(testInfo.project.name)
      await page.goto('/my-tasks')
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(500)
      await page.screenshot({
        path: getScreenshotPath(device, 'my-tasks', 'main.png'),
        fullPage: device === 'mobile',
      })
    })
  })
})
