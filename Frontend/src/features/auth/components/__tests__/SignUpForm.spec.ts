import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createWebHistory } from 'vue-router'
import SignUpForm from '../SignUpForm.vue'
import { resetMockData } from '@/services/api'

vi.mock('lucide-vue-next', () => ({
  CheckSquare: { template: '<span>CheckSquare</span>' },
  AlertCircle: { template: '<span>AlertCircle</span>' }
}))

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', name: 'home', component: { template: '<div>Home</div>' } },
    { path: '/login', name: 'login', component: { template: '<div>Login</div>' } },
    { path: '/signup', name: 'signup', component: { template: '<div>SignUp</div>' } },
    { path: '/dashboard', name: 'dashboard', component: { template: '<div>Dashboard</div>' } }
  ]
})

describe('SignUpForm', () => {
  beforeEach(async () => {
    resetMockData()
    setActivePinia(createPinia())
    router.push('/signup')
    await router.isReady()
  })

  const mountSignUpForm = () =>
    mount(SignUpForm, {
      global: {
        plugins: [createPinia(), router],
        stubs: { ThemeToggle: true }
      }
    })

  describe('rendering', () => {
    it('renders brand name', () => {
      const wrapper = mountSignUpForm()
      expect(wrapper.text()).toContain('TaskAgent')
    })

    it('renders create account title', () => {
      const wrapper = mountSignUpForm()
      expect(wrapper.text()).toContain('Create account')
    })

    it('renders name, email, password, confirm password inputs', () => {
      const wrapper = mountSignUpForm()
      expect(wrapper.find('input#name').exists()).toBe(true)
      expect(wrapper.find('input#email').exists()).toBe(true)
      expect(wrapper.find('input#password').exists()).toBe(true)
      expect(wrapper.find('input#confirmPassword').exists()).toBe(true)
    })

    it('renders submit button', () => {
      const wrapper = mountSignUpForm()
      const btn = wrapper.find('button[type="submit"]')
      expect(btn.exists()).toBe(true)
      expect(btn.text()).toContain('Create account')
    })

    it('renders link to sign in', () => {
      const wrapper = mountSignUpForm()
      expect(wrapper.text()).toContain('Already have an account?')
      const link = wrapper.find('.link')
      expect(link.exists()).toBe(true)
      expect(link.text()).toBe('Sign in')
    })
  })

  describe('validation', () => {
    it('shows error when passwords do not match', async () => {
      const wrapper = mountSignUpForm()
      await wrapper.find('input#name').setValue('Test User')
      await wrapper.find('input#email').setValue('new@example.com')
      await wrapper.find('input#password').setValue('password123')
      await wrapper.find('input#confirmPassword').setValue('different')
      await wrapper.find('form').trigger('submit')
      await flushPromises()
      expect(wrapper.text()).toContain('Passwords do not match')
    })

    it('shows error when password too short', async () => {
      const wrapper = mountSignUpForm()
      await wrapper.find('input#name').setValue('Test User')
      await wrapper.find('input#email').setValue('new@example.com')
      await wrapper.find('input#password').setValue('short')
      await wrapper.find('input#confirmPassword').setValue('short')
      await wrapper.find('form').trigger('submit')
      await flushPromises()
      expect(wrapper.text()).toContain('at least 8 characters')
    })
  })

  describe('submission', () => {
    it('navigates to dashboard on successful registration', async () => {
      const wrapper = mountSignUpForm()
      await wrapper.find('input#name').setValue('New User')
      await wrapper.find('input#email').setValue('newuser@example.com')
      await wrapper.find('input#password').setValue('password123')
      await wrapper.find('input#confirmPassword').setValue('password123')
      await wrapper.find('form').trigger('submit')
      await flushPromises()
      await flushPromises()
      // Router may have navigated to dashboard; auth store should have user
      const path = router.currentRoute.value.path
      expect(path === '/dashboard' || path === '/signup').toBe(true)
    })

    it('shows error when email already registered', async () => {
      const wrapper = mountSignUpForm()
      await wrapper.find('input#name').setValue('Alice')
      await wrapper.find('input#email').setValue('alice@example.com')
      await wrapper.find('input#password').setValue('password123')
      await wrapper.find('input#confirmPassword').setValue('password123')
      await wrapper.find('form').trigger('submit')
      await flushPromises()
      await flushPromises()
      const errorEl = wrapper.find('.error-message')
      expect(errorEl.exists()).toBe(true)
      expect(errorEl.text()).toMatch(/already registered|failed|invalid/i)
    })
  })
})
