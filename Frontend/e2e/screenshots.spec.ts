import { test } from '@playwright/test'
import { mkdirSync, existsSync } from 'node:fs'
import { join } from 'node:path'

const SCREENSHOTS_BASE = 'screenshots'

// Light mode only
const DEMO_USER = {
  id: 1,
  name: 'Alice Johnson',
  email: 'alice@example.com',
  avatar: '👩‍💼',
}

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

async function setLightTheme(page: import('@playwright/test').Page) {
  await page.evaluate(() => {
    localStorage.setItem('theme', JSON.stringify({ theme: 'light' }))
    document.documentElement.classList.remove('dark')
    document.documentElement.classList.add('light')
  })
}

async function setupAuth(page: import('@playwright/test').Page) {
  await page.goto('/login')
  await page.evaluate(
    (u) => {
      // Auth store requires both user and token; pinia-plugin-persistedstate key is 'auth'
      localStorage.setItem('auth', JSON.stringify({ user: u, token: 'mock-jwt-for-screenshots' }))
      localStorage.setItem('theme', JSON.stringify({ theme: 'light' }))
      document.documentElement.classList.remove('dark')
      document.documentElement.classList.add('light')
    },
    DEMO_USER
  )
  await page.reload()
  await page.waitForLoadState('networkidle')
}

test.describe('Screenshots by device (light mode)', () => {
  test.beforeEach(async ({ page }, testInfo) => {
    const device = getDeviceFromProject(testInfo.project.name)
    await page.setViewportSize(DEVICE_VIEWPORTS[device])
  })

  test.describe('auth', () => {
    test('login', async ({ page }, testInfo) => {
      const device = getDeviceFromProject(testInfo.project.name)
      await page.goto('/login')
      await page.waitForLoadState('networkidle')
      await setLightTheme(page)
      await page.waitForTimeout(300)
      await page.screenshot({
        path: getScreenshotPath(device, 'auth', 'login.png'),
        fullPage: device === 'mobile',
      })
    })

    test('signup', async ({ page }, testInfo) => {
      const device = getDeviceFromProject(testInfo.project.name)
      await page.goto('/signup')
      await page.waitForLoadState('networkidle')
      await setLightTheme(page)
      await page.waitForTimeout(300)
      await page.screenshot({
        path: getScreenshotPath(device, 'auth', 'signup.png'),
        fullPage: device === 'mobile',
      })
    })
  })

  test.describe('dashboard', () => {
    test.beforeEach(async ({ page }) => {
      await setupAuth(page)
    })

    test('list view', async ({ page }, testInfo) => {
      const device = getDeviceFromProject(testInfo.project.name)
      await page.evaluate(() => localStorage.setItem('taskViewMode', 'list'))
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
      await page.evaluate(() => localStorage.setItem('taskViewMode', 'columns'))
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
      await page.evaluate(() => localStorage.setItem('taskViewMode', 'analytics'))
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
      try {
        await newTaskBtn.waitFor({ state: 'visible', timeout: 3000 })
      } catch {
        testInfo.skip(true, 'No project selected – New task button not available')
        return
      }
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
      await setupAuth(page)
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

  test.describe('profile', () => {
    test.beforeEach(async ({ page }) => {
      await setupAuth(page)
    })

    test('profile view', async ({ page }, testInfo) => {
      const device = getDeviceFromProject(testInfo.project.name)
      await page.goto('/profile')
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(500)
      await page.screenshot({
        path: getScreenshotPath(device, 'profile', 'profile.png'),
        fullPage: device === 'mobile',
      })
    })
  })

  test.describe('project-settings', () => {
    test.beforeEach(async ({ page }) => {
      await setupAuth(page)
    })

    test('project settings view', async ({ page }, testInfo) => {
      const device = getDeviceFromProject(testInfo.project.name)
      await page.goto('/project-settings')
      await page.waitForLoadState('networkidle')
      await page.waitForTimeout(500)
      await page.screenshot({
        path: getScreenshotPath(device, 'project-settings', 'project-settings.png'),
        fullPage: device === 'mobile',
      })
    })
  })
})
