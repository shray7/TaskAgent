import { test } from '@playwright/test'

// Configure screenshots directory
const screenshotsDir = 'screenshots'

// Demo user for login
const demoUser = {
  id: 1,
  name: 'Alice Johnson',
  email: 'alice@example.com',
  avatar: '👩‍💼'
}

test.describe('App Feature Screenshots', () => {
  test.beforeEach(async ({ page }) => {
    // Set a consistent viewport for all screenshots
    await page.setViewportSize({ width: 1440, height: 900 })
  })

  test('01 - Login Page Light Theme', async ({ page }) => {
    await page.goto('/login')
    await page.waitForLoadState('networkidle')
    
    // Ensure light theme
    await page.evaluate(() => {
      document.documentElement.classList.remove('dark')
      document.documentElement.classList.add('light')
      localStorage.setItem('theme', 'light')
    })
    await page.waitForTimeout(300)
    
    await page.screenshot({ 
      path: `${screenshotsDir}/01-login-light.png`,
      fullPage: false 
    })
  })

  test('02 - Login Page Dark Theme', async ({ page }) => {
    await page.goto('/login')
    await page.waitForLoadState('networkidle')
    
    // Switch to dark theme
    await page.evaluate(() => {
      document.documentElement.classList.remove('light')
      document.documentElement.classList.add('dark')
      localStorage.setItem('theme', 'dark')
    })
    await page.waitForTimeout(300)
    
    await page.screenshot({ 
      path: `${screenshotsDir}/02-login-dark.png`,
      fullPage: false 
    })
  })

  test('03 - Dashboard List View Light Theme', async ({ page }) => {
    // Set up authentication and preferences via localStorage before navigating
    await page.goto('/login')
    await page.evaluate((user) => {
      localStorage.setItem('user', JSON.stringify(user))
      localStorage.setItem('theme', 'light')
      localStorage.setItem('taskViewMode', 'list')
      document.documentElement.classList.remove('dark')
      document.documentElement.classList.add('light')
    }, demoUser)
    
    // Navigate to dashboard - auth store will pick up user from localStorage
    await page.goto('/dashboard')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(500)
    
    await page.screenshot({ 
      path: `${screenshotsDir}/03-dashboard-list-light.png`,
      fullPage: false 
    })
  })

  test('04 - Dashboard List View Dark Theme', async ({ page }) => {
    await page.goto('/login')
    await page.evaluate((user) => {
      localStorage.setItem('user', JSON.stringify(user))
      localStorage.setItem('theme', 'dark')
      localStorage.setItem('taskViewMode', 'list')
      document.documentElement.classList.remove('light')
      document.documentElement.classList.add('dark')
    }, demoUser)
    
    await page.goto('/dashboard')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(500)
    
    await page.screenshot({ 
      path: `${screenshotsDir}/04-dashboard-list-dark.png`,
      fullPage: false 
    })
  })

  test('05 - Dashboard Column View Light Theme', async ({ page }) => {
    await page.goto('/login')
    await page.evaluate((user) => {
      localStorage.setItem('user', JSON.stringify(user))
      localStorage.setItem('theme', 'light')
      localStorage.setItem('taskViewMode', 'columns')
      document.documentElement.classList.remove('dark')
      document.documentElement.classList.add('light')
    }, demoUser)
    
    await page.goto('/dashboard')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(500)
    
    await page.screenshot({ 
      path: `${screenshotsDir}/05-dashboard-columns-light.png`,
      fullPage: false 
    })
  })

  test('06 - Dashboard Column View Dark Theme', async ({ page }) => {
    await page.goto('/login')
    await page.evaluate((user) => {
      localStorage.setItem('user', JSON.stringify(user))
      localStorage.setItem('theme', 'dark')
      localStorage.setItem('taskViewMode', 'columns')
      document.documentElement.classList.remove('light')
      document.documentElement.classList.add('dark')
    }, demoUser)
    
    await page.goto('/dashboard')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(500)
    
    await page.screenshot({ 
      path: `${screenshotsDir}/06-dashboard-columns-dark.png`,
      fullPage: false 
    })
  })

  test('07 - New Task Form Modal', async ({ page }) => {
    await page.goto('/login')
    await page.evaluate((user) => {
      localStorage.setItem('user', JSON.stringify(user))
      localStorage.setItem('theme', 'light')
    }, demoUser)
    
    await page.goto('/dashboard')
    await page.waitForLoadState('networkidle')
    
    // Wait for the button to be visible
    const newTaskBtn = page.locator('button.btn-primary:has(span)')
    await newTaskBtn.waitFor({ state: 'visible', timeout: 10000 })
    await newTaskBtn.click()
    await page.waitForTimeout(300)
    
    await page.screenshot({ 
      path: `${screenshotsDir}/07-task-form-modal.png`,
      fullPage: false 
    })
  })

  test('08 - Inline Edit Title', async ({ page }) => {
    await page.goto('/login')
    await page.evaluate((user) => {
      localStorage.setItem('user', JSON.stringify(user))
      localStorage.setItem('theme', 'light')
    }, demoUser)
    
    await page.goto('/dashboard')
    await page.waitForLoadState('networkidle')
    
    // Wait for task cards to be visible
    const taskTitle = page.locator('.task-title.editable').first()
    await taskTitle.waitFor({ state: 'visible', timeout: 10000 })
    await taskTitle.click()
    await page.waitForTimeout(300)
    
    await page.screenshot({ 
      path: `${screenshotsDir}/08-inline-edit-title.png`,
      fullPage: false 
    })
  })

  test('09 - Inline Edit Assignee', async ({ page }) => {
    await page.goto('/login')
    await page.evaluate((user) => {
      localStorage.setItem('user', JSON.stringify(user))
      localStorage.setItem('theme', 'light')
    }, demoUser)
    
    await page.goto('/dashboard')
    await page.waitForLoadState('networkidle')
    
    // Wait for task cards to be visible
    const assigneeSpan = page.locator('.meta-item .editable').first()
    await assigneeSpan.waitFor({ state: 'visible', timeout: 10000 })
    await assigneeSpan.click()
    await page.waitForTimeout(300)
    
    await page.screenshot({ 
      path: `${screenshotsDir}/09-inline-edit-assignee.png`,
      fullPage: false 
    })
  })

  test('10 - Inline Edit Due Date', async ({ page }) => {
    await page.goto('/login')
    await page.evaluate((user) => {
      localStorage.setItem('user', JSON.stringify(user))
      localStorage.setItem('theme', 'light')
    }, demoUser)
    
    await page.goto('/dashboard')
    await page.waitForLoadState('networkidle')
    
    // Wait for task cards to be visible - click on the date (second meta-item's editable)
    const dateSpan = page.locator('.task-card').first().locator('.meta-item').nth(1).locator('.editable')
    await dateSpan.waitFor({ state: 'visible', timeout: 10000 })
    await dateSpan.click()
    await page.waitForTimeout(300)
    
    await page.screenshot({ 
      path: `${screenshotsDir}/10-inline-edit-date.png`,
      fullPage: false 
    })
  })

  test('11 - Mobile View Dashboard', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 812 })
    
    await page.goto('/login')
    await page.evaluate((user) => {
      localStorage.setItem('user', JSON.stringify(user))
      localStorage.setItem('theme', 'light')
    }, demoUser)
    
    await page.goto('/dashboard')
    await page.waitForLoadState('networkidle')
    await page.waitForTimeout(300)
    
    await page.screenshot({ 
      path: `${screenshotsDir}/11-mobile-dashboard.png`,
      fullPage: true 
    })
  })

  test('12 - Mobile View Login', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 812 })
    
    await page.goto('/login')
    await page.waitForLoadState('networkidle')
    
    // Set light theme
    await page.evaluate(() => {
      document.documentElement.classList.remove('dark')
      document.documentElement.classList.add('light')
      localStorage.setItem('theme', 'light')
    })
    await page.waitForTimeout(300)
    
    await page.screenshot({ 
      path: `${screenshotsDir}/12-mobile-login.png`,
      fullPage: false 
    })
  })
})
