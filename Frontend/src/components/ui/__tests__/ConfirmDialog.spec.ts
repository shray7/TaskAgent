import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import ConfirmDialog from '../ConfirmDialog.vue'

vi.mock('lucide-vue-next', () => ({
  AlertTriangle: { template: '<span data-icon="alert-triangle"></span>' },
  AlertCircle: { template: '<span data-icon="alert-circle"></span>' }
}))

// Teleport renders to body; stub it so content stays in wrapper
const teleportStub = {
  template: '<div class="teleport-stub"><slot /></div>'
}

function mountDialog(props: Record<string, unknown> = {}) {
  return mount(ConfirmDialog, {
    props: { isOpen: true, title: 'T', message: 'M', ...props },
    global: { stubs: { Teleport: teleportStub, Transition: { template: '<slot />' } } }
  })
}

describe('ConfirmDialog', () => {
  it('renders nothing when isOpen is false', () => {
    const wrapper = mount(ConfirmDialog, {
      props: { isOpen: false, title: 'Confirm', message: 'Are you sure?' },
      global: { stubs: { Teleport: teleportStub, Transition: { template: '<slot />' } } }
    })
    expect(wrapper.find('.confirm-overlay').exists()).toBe(false)
  })

  it('renders overlay and content when isOpen is true', () => {
    const wrapper = mountDialog({ title: 'Delete?', message: 'This cannot be undone.' })
    expect(wrapper.find('.confirm-overlay').exists()).toBe(true)
    expect(wrapper.find('.confirm-title').text()).toBe('Delete?')
    expect(wrapper.find('.confirm-message').text()).toBe('This cannot be undone.')
  })

  it('shows description when provided', () => {
    const wrapper = mountDialog({ description: 'Extra details' })
    expect(wrapper.find('.confirm-description').text()).toBe('Extra details')
  })

  it('uses default confirm and cancel text', () => {
    const wrapper = mountDialog()
    const buttons = wrapper.findAll('.confirm-actions button')
    expect(buttons[0]?.text()).toBe('Cancel')
    expect(buttons[1]?.text()).toBe('Confirm')
  })

  it('uses custom confirm and cancel text when provided', () => {
    const wrapper = mountDialog({ confirmText: 'Yes, delete', cancelText: 'Keep' })
    const buttons = wrapper.findAll('.confirm-actions button')
    expect(buttons[0]?.text()).toBe('Keep')
    expect(buttons[1]?.text()).toBe('Yes, delete')
  })

  it('emits confirm when confirm button clicked', async () => {
    const wrapper = mountDialog()
    await wrapper.findAll('.confirm-actions button')[1]?.trigger('click')
    expect(wrapper.emitted('confirm')).toHaveLength(1)
  })

  it('emits cancel when cancel button clicked', async () => {
    const wrapper = mountDialog()
    await wrapper.findAll('.confirm-actions button')[0]?.trigger('click')
    expect(wrapper.emitted('cancel')).toHaveLength(1)
  })

  it('applies danger variant class', () => {
    const wrapper = mountDialog({ variant: 'danger' })
    expect(wrapper.find('.confirm-dialog.danger').exists()).toBe(true)
  })
})
