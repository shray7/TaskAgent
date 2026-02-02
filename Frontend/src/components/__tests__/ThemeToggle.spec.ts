import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import ThemeToggle from '../ThemeToggle.vue'
import { useThemeStore } from '@/stores/theme'

// Mock lucide-vue-next icons
vi.mock('lucide-vue-next', () => ({
  Sun: { template: '<span class="sun-icon">Sun</span>' },
  Moon: { template: '<span class="moon-icon">Moon</span>' }
}))

// Mock localStorage and document
const localStorageMock = {
  getItem: vi.fn(),
  setItem: vi.fn(),
  removeItem: vi.fn()
}
Object.defineProperty(window, 'localStorage', { value: localStorageMock })

const classListMock = {
  add: vi.fn(),
  remove: vi.fn()
}
Object.defineProperty(document.documentElement, 'classList', { value: classListMock })

describe('ThemeToggle', () => {
  let pinia: ReturnType<typeof createPinia>

  beforeEach(() => {
    pinia = createPinia()
    setActivePinia(pinia)
    vi.clearAllMocks()
  })

  const mountThemeToggle = (props = {}) => {
    return mount(ThemeToggle, {
      props: {
        ...props
      },
      global: {
        plugins: [pinia],
        stubs: {
          Sun: { template: '<span class="sun-icon">Sun</span>' },
          Moon: { template: '<span class="moon-icon">Moon</span>' }
        }
      }
    })
  }

  describe('rendering', () => {
    it('should render a button', () => {
      const wrapper = mountThemeToggle()
      expect(wrapper.find('button').exists()).toBe(true)
    })

    it('should show Sun or Moon icon based on theme', () => {
      const themeStore = useThemeStore()
      themeStore.setTheme('light')
      const wrapper = mountThemeToggle()
      const hasSun = wrapper.find('.sun-icon').exists()
      const hasMoon = wrapper.find('.moon-icon').exists()
      expect(hasSun || hasMoon).toBe(true)
    })

    it('should show Moon icon in dark mode', async () => {
      const themeStore = useThemeStore()
      themeStore.setTheme('dark')
      const wrapper = mountThemeToggle()
      expect(wrapper.find('.moon-icon').exists()).toBe(true)
    })

    it('should not show label by default', () => {
      const wrapper = mountThemeToggle()
      expect(wrapper.find('.label').exists()).toBe(false)
    })

    it('should show label when showLabel prop is true', () => {
      const wrapper = mountThemeToggle({ showLabel: true })
      expect(wrapper.find('.label').exists()).toBe(true)
    })
  })

  describe('interaction', () => {
    it('should toggle theme when clicked', async () => {
      const themeStore = useThemeStore()
      themeStore.setTheme('light')
      const wrapper = mountThemeToggle()
      await wrapper.find('button').trigger('click')
      expect(themeStore.theme).toBe('dark')
    })

    it('should toggle between light and dark only', async () => {
      const themeStore = useThemeStore()
      themeStore.setTheme('light')
      const wrapper = mountThemeToggle()
      await wrapper.find('button').trigger('click')
      expect(themeStore.theme).toBe('dark')
      await wrapper.find('button').trigger('click')
      expect(themeStore.theme).toBe('light')
    })
  })

  describe('accessibility', () => {
    it('should have aria-label', () => {
      const wrapper = mountThemeToggle()
      expect(wrapper.find('button').attributes('aria-label')).toBe('Toggle theme')
    })

    it('should have title attribute with current mode', () => {
      const themeStore = useThemeStore()
      themeStore.setTheme('light')
      const wrapper = mountThemeToggle()
      expect(wrapper.find('button').attributes('title')).toContain('Light')
    })
  })

  describe('label display', () => {
    it('should show "Light" label in light mode', () => {
      const themeStore = useThemeStore()
      themeStore.setTheme('light')
      const wrapper = mountThemeToggle({ showLabel: true })
      expect(wrapper.find('.label').text()).toBe('Light')
    })

    it('should show "Dark" label in dark mode', () => {
      const themeStore = useThemeStore()
      themeStore.setTheme('dark')
      const wrapper = mountThemeToggle({ showLabel: true })
      expect(wrapper.find('.label').text()).toBe('Dark')
    })

  })
})
