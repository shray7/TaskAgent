import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import TaskForm from '../TaskForm.vue'

import { useProjectsStore } from '@/stores/projects'
import { useSprintsStore } from '@/stores/sprints'
import { resetMockData } from '@/services/api'

// Mock lucide-vue-next icons
vi.mock('lucide-vue-next', () => ({
  X: { template: '<span>X</span>' },
  Plus: { template: '<span>Plus</span>' },
  Edit2: { template: '<span>Edit2</span>' }
}))

const mockTask = {
  id: 1,
  title: 'Existing Task',
  description: 'Existing description',
  status: 'in-progress' as const,
  priority: 'high' as const,
  assigneeId: 2,
  dueDate: new Date('2024-02-15'),
  tags: ['backend', 'api']
}

describe('TaskForm', () => {
  let pinia: ReturnType<typeof createPinia>

  beforeEach(async () => {
    resetMockData()
    pinia = createPinia()
    setActivePinia(pinia)
    const projectsStore = useProjectsStore()
    const sprintsStore = useSprintsStore()
    await projectsStore.fetchProjects()
    projectsStore.setCurrentProject(1)
    await sprintsStore.fetchSprints(1)
  })

  const mountTaskForm = (props = {}) => {
    return mount(TaskForm, {
      props: {
        ...props
      },
      global: {
        plugins: [pinia],
        stubs: {
          X: true,
          Plus: true,
          Edit2: true
        }
      }
    })
  }

  describe('create mode', () => {
    it('should show "Create Task" title when no task prop', () => {
      const wrapper = mountTaskForm()
      expect(wrapper.text()).toContain('Create Task')
    })

    it('should have empty form fields', () => {
      const wrapper = mountTaskForm()
      const titleInput = wrapper.find('input[type="text"]')
      expect((titleInput.element as HTMLInputElement).value).toBe('')
    })

    it('should have default priority of medium', () => {
      const wrapper = mountTaskForm()
      const prioritySelect = wrapper.findAll('select')[0]
      expect(prioritySelect).toBeDefined()
      expect((prioritySelect!.element as HTMLSelectElement).value).toBe('medium')
    })

    it('should have default status of todo', () => {
      const wrapper = mountTaskForm()
      const statusSelect = wrapper.findAll('select')[1]
      expect(statusSelect).toBeDefined()
      expect((statusSelect!.element as HTMLSelectElement).value).toBe('todo')
    })

    it('should show "Create Task" on submit button', () => {
      const wrapper = mountTaskForm()
      const submitButton = wrapper.find('button[type="submit"]')
      expect(submitButton.text()).toContain('Create Task')
    })
  })

  describe('edit mode', () => {
    it('should show "Edit Task" title when task prop provided', () => {
      const wrapper = mountTaskForm({ task: mockTask })
      expect(wrapper.text()).toContain('Edit Task')
    })

    it('should populate form with task data', () => {
      const wrapper = mountTaskForm({ task: mockTask })
      
      const titleInput = wrapper.find('input[type="text"]')
      expect((titleInput.element as HTMLInputElement).value).toBe('Existing Task')
    })

    it('should populate description', () => {
      const wrapper = mountTaskForm({ task: mockTask })
      const textarea = wrapper.find('textarea')
      expect((textarea.element as HTMLTextAreaElement).value).toBe('Existing description')
    })

    it('should populate tags input', () => {
      const wrapper = mountTaskForm({ task: mockTask })
      const tagsInput = wrapper.findAll('input[type="text"]')[1]
      expect(tagsInput).toBeDefined()
      expect((tagsInput!.element as HTMLInputElement).value).toBe('backend, api')
    })

    it('should show "Update Task" on submit button', () => {
      const wrapper = mountTaskForm({ task: mockTask })
      const submitButton = wrapper.find('button[type="submit"]')
      expect(submitButton.text()).toContain('Update Task')
    })
  })

  describe('form interaction', () => {
    it('should update title when typing', async () => {
      const wrapper = mountTaskForm()
      const titleInput = wrapper.find('input[type="text"]')
      
      await titleInput.setValue('New Task Title')
      
      expect((titleInput.element as HTMLInputElement).value).toBe('New Task Title')
    })

    it('should update description when typing', async () => {
      const wrapper = mountTaskForm()
      const textarea = wrapper.find('textarea')
      
      await textarea.setValue('New description')
      
      expect((textarea.element as HTMLTextAreaElement).value).toBe('New description')
    })

    it('should update priority selection', async () => {
      const wrapper = mountTaskForm()
      const prioritySelect = wrapper.findAll('select')[0]
      if (!prioritySelect) return
      await prioritySelect.setValue('high')
      expect((prioritySelect.element as HTMLSelectElement).value).toBe('high')
    })
  })

  describe('form submission', () => {
    it('should emit submit event with form data', async () => {
      const wrapper = mountTaskForm()
      const titleInput = wrapper.find('input[type="text"]')
      const form = wrapper.find('form')
      
      await titleInput.setValue('Test Task')
      await form.trigger('submit')
      await flushPromises()
      
      expect(wrapper.emitted('submit')).toBeTruthy()
      const payload = wrapper.emitted('submit')?.[0]?.[0] as any
      expect(payload?.title).toBe('Test Task')
    })

    it('should parse tags correctly', async () => {
      const wrapper = mountTaskForm()
      const titleInput = wrapper.find('input[type="text"]')
      const tagsInput = wrapper.findAll('input[type="text"]')[1]
      const form = wrapper.find('form')
      if (!tagsInput) return
      await titleInput.setValue('Test Task')
      await tagsInput.setValue('tag1, tag2, tag3')
      await form.trigger('submit')
      await flushPromises()
      const emittedData = wrapper.emitted('submit')?.[0]?.[0] as any
      expect(emittedData?.tags).toEqual(['tag1', 'tag2', 'tag3'])
    })

    it('should filter empty tags', async () => {
      const wrapper = mountTaskForm()
      const titleInput = wrapper.find('input[type="text"]')
      const tagsInput = wrapper.findAll('input[type="text"]')[1]
      const form = wrapper.find('form')
      if (!tagsInput) return
      await titleInput.setValue('Test Task')
      await tagsInput.setValue('tag1, , tag2, ')
      await form.trigger('submit')
      await flushPromises()
      const emittedData = wrapper.emitted('submit')?.[0]?.[0] as any
      expect(emittedData?.tags).toEqual(['tag1', 'tag2'])
    })
  })

  describe('modal behavior', () => {
    it('should emit close when close button clicked', async () => {
      const wrapper = mountTaskForm()
      const closeButton = wrapper.find('.close-btn')
      
      await closeButton.trigger('click')
      
      expect(wrapper.emitted('close')).toBeTruthy()
    })

    it('should emit close when cancel button clicked', async () => {
      const wrapper = mountTaskForm()
      const cancelButton = wrapper.find('button[type="button"]')
      
      await cancelButton.trigger('click')
      
      expect(wrapper.emitted('close')).toBeTruthy()
    })

    it('should emit close when overlay clicked', async () => {
      const wrapper = mountTaskForm()
      const overlay = wrapper.find('.modal-overlay')
      
      await overlay.trigger('click')
      
      expect(wrapper.emitted('close')).toBeTruthy()
    })

    it('should not close when modal content clicked', async () => {
      const wrapper = mountTaskForm()
      const modal = wrapper.find('.modal')
      
      await modal.trigger('click')
      
      // Close should not be emitted when clicking inside modal
      expect(wrapper.emitted('close')).toBeFalsy()
    })
  })
})
