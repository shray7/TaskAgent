import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { Sprint, SprintStatus } from '@/types'
import { sprintDurationMs } from '@/types'
import { api } from '@/services/api'
import { useProjectsStore } from './projects'

export const useSprintsStore = defineStore(
  'sprints',
  () => {
    const sprints = ref<Sprint[]>([])
    const currentSprintId = ref<number | null>(null)
    const loading = ref(false)
    const loaded = ref(false)

    const currentSprint = computed(() =>
      currentSprintId.value ? sprints.value.find(s => s.id === currentSprintId.value) : null
    )

    async function fetchSprints(projectId?: number): Promise<void> {
      if (loading.value) return
      loading.value = true
      try {
        sprints.value = await api.sprints.getAll(projectId)
        loaded.value = true
      } catch {
        sprints.value = []
      } finally {
        loading.value = false
      }
    }

    function getSprintsByProject(projectId: number): Sprint[] {
      return sprints.value.filter(s => s.projectId === projectId)
    }

    function getActiveSprintForProject(projectId: number): Sprint | undefined {
      return sprints.value.find(s => s.projectId === projectId && s.status === 'active')
    }

    function setCurrentSprint(sprintId: number | null): void {
      currentSprintId.value = sprintId
    }

    async function createSprint(projectId: number, name: string, goal: string, startDate: Date): Promise<Sprint> {
      const projectsStore = useProjectsStore()
      const newSprint = await api.sprints.create({ projectId, name, goal, startDate })
      sprints.value.push(newSprint)
      return newSprint
    }

    async function updateSprint(sprintId: number, updates: Partial<Sprint>): Promise<Sprint | null> {
      const sprint = sprints.value.find(s => s.id === sprintId)
      if (!sprint) return null
      if (updates.startDate && !updates.endDate) {
        const projectsStore = useProjectsStore()
        const durationDays = projectsStore.getSprintDurationDays(sprint.projectId)
        updates.endDate = new Date(updates.startDate.getTime() + sprintDurationMs(durationDays))
      }
      const updated = await api.sprints.update(sprintId, updates)
      if (updated) {
        const idx = sprints.value.findIndex(s => s.id === sprintId)
        if (idx !== -1) sprints.value[idx] = updated
        return updated
      }
      return null
    }

    async function updateSprintStatus(sprintId: number, status: SprintStatus): Promise<Sprint | null> {
      return updateSprint(sprintId, { status })
    }

    async function startSprint(sprintId: number): Promise<Sprint | null> {
      const sprint = sprints.value.find(s => s.id === sprintId)
      if (!sprint || sprint.status !== 'planning') return null
      const activeSprint = getActiveSprintForProject(sprint.projectId)
      if (activeSprint) {
        await api.sprints.complete(activeSprint.id)
        const idx = sprints.value.findIndex(s => s.id === activeSprint.id)
        if (idx !== -1) sprints.value[idx] = { ...sprints.value[idx]!, status: 'completed' }
      }
      const updated = await api.sprints.start(sprintId)
      if (updated) {
        const idx = sprints.value.findIndex(s => s.id === sprintId)
        if (idx !== -1) sprints.value[idx] = updated
        return updated
      }
      return null
    }

    async function completeSprint(sprintId: number): Promise<Sprint | null> {
      const sprint = sprints.value.find(s => s.id === sprintId)
      if (!sprint || sprint.status !== 'active') return null
      const updated = await api.sprints.complete(sprintId)
      if (updated) {
        const idx = sprints.value.findIndex(s => s.id === sprintId)
        if (idx !== -1) sprints.value[idx] = updated
        return updated
      }
      return null
    }

    async function deleteSprint(sprintId: number): Promise<void> {
      await api.sprints.delete(sprintId)
      sprints.value = sprints.value.filter(s => s.id !== sprintId)
      if (currentSprintId.value === sprintId) currentSprintId.value = null
    }

    function getSprintById(sprintId: number): Sprint | undefined {
      return sprints.value.find(s => s.id === sprintId)
    }

    function getSprintProgress(sprintId: number): { daysElapsed: number; daysRemaining: number; percentComplete: number } {
      const sprint = sprints.value.find(s => s.id === sprintId)
      if (!sprint) return { daysElapsed: 0, daysRemaining: 0, percentComplete: 0 }
      const now = new Date()
      const start = new Date(sprint.startDate)
      const end = new Date(sprint.endDate)
      const totalDays = Math.ceil((end.getTime() - start.getTime()) / (1000 * 60 * 60 * 24))
      const daysElapsed = Math.max(0, Math.ceil((now.getTime() - start.getTime()) / (1000 * 60 * 60 * 24)))
      const daysRemaining = Math.max(0, Math.ceil((end.getTime() - now.getTime()) / (1000 * 60 * 60 * 24)))
      const percentComplete = Math.min(100, Math.max(0, (daysElapsed / totalDays) * 100))
      return { daysElapsed, daysRemaining, percentComplete }
    }

    function initializeSprints(projectId: number): void {
      const activeSprint = getActiveSprintForProject(projectId)
      if (currentSprintId.value != null) {
        const sprint = sprints.value.find(s => s.id === currentSprintId.value)
        if (sprint && sprint.projectId === projectId) return
      }
      currentSprintId.value = activeSprint?.id ?? null
    }

    return {
      sprints,
      currentSprintId,
      currentSprint,
      loading,
      loaded,
      fetchSprints,
      getSprintsByProject,
      getActiveSprintForProject,
      setCurrentSprint,
      createSprint,
      updateSprint,
      updateSprintStatus,
      startSprint,
      completeSprint,
      deleteSprint,
      getSprintById,
      getSprintProgress,
      initializeSprints
    }
  },
  {
    persist: { paths: ['currentSprintId'] }
  }
)
