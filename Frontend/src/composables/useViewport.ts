import { ref, onMounted, onUnmounted } from 'vue'

export type ViewportSize = 'desktop' | 'tablet' | 'mobile'

const BREAKPOINTS = {
  tablet: 768,
  desktop: 1024
} as const

function getViewportSize(): ViewportSize {
  if (typeof window === 'undefined') return 'desktop'
  const w = window.innerWidth
  if (w >= BREAKPOINTS.desktop) return 'desktop'
  if (w >= BREAKPOINTS.tablet) return 'tablet'
  return 'mobile'
}

export function useViewport() {
  const viewport = ref<ViewportSize>(getViewportSize())

  function update() {
    viewport.value = getViewportSize()
  }

  onMounted(() => {
    update()
    window.addEventListener('resize', update)
  })

  onUnmounted(() => {
    window.removeEventListener('resize', update)
  })

  return {
    viewport,
    isDesktop: () => viewport.value === 'desktop',
    isTablet: () => viewport.value === 'tablet',
    isMobile: () => viewport.value === 'mobile'
  }
}
