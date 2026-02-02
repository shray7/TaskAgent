import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { Project, TaskStatus } from '@/types'
import { DEFAULT_SPRINT_DURATION_DAYS } from '@/types'
import { api } from '@/services/api'

const defaultColumns: TaskStatus[] = ['todo', 'in-progress', 'completed']

export const useProjectsStore = defineStore(
  'projects',
  () => {
    const projects = ref<Project[]>([])
    const currentProjectId = ref<number>(1)
    const switchToDashboard = ref(false)
    const loading = ref(false)
    const loaded = ref(false)

    const currentProject = computed(() =>
      projects.value.find(p => p.id === currentProjectId.value)
    )

    async function fetchProjects(): Promise<void> {
      if (loading.value) return
      loading.value = true
      try {
        projects.value = await api.projects.getAll()
        loaded.value = true
      } catch {
        projects.value = []
      } finally {
        loading.value = false
      }
    }

    function setCurrentProject(projectId: number): void {
      currentProjectId.value = projectId
    }

    function setSwitchToDashboard(value: boolean): void {
      switchToDashboard.value = value
    }

    async function addProject(projectData: Omit<Project, 'id' | 'createdAt'>): Promise<Project> {
      const newProject = await api.projects.create(projectData)
      projects.value.push(newProject)
      return newProject
    }

    async function updateProject(projectId: number, updates: Partial<Project>): Promise<Project | null> {
      const updated = await api.projects.update(projectId, updates)
      if (updated) {
        const idx = projects.value.findIndex(p => p.id === projectId)
        if (idx !== -1) projects.value[idx] = updated
        return updated
      }
      return null
    }

    async function deleteProject(projectId: number, userId: number): Promise<{ success: boolean; message?: string }> {
      const result = await api.projects.delete(projectId, userId)
      if (result.success) {
        projects.value = projects.value.filter(p => p.id !== projectId)
        const first = projects.value[0]
        if (currentProjectId.value === projectId && first) {
          currentProjectId.value = first.id
        }
      }
      return result
    }

    function getProjectById(projectId: number): Project | undefined {
      return projects.value.find(p => p.id === projectId)
    }

    function getSprintDurationDays(projectId: number): number {
      const project = projects.value.find(p => p.id === projectId)
      return project?.sprintDurationDays ?? DEFAULT_SPRINT_DURATION_DAYS
    }

    function getVisibleColumns(projectId: number): TaskStatus[] {
      const project = projects.value.find(p => p.id === projectId)
      return project?.visibleColumns?.length ? project.visibleColumns : defaultColumns
    }

    function getTaskSizeUnit(projectId: number): 'hours' | 'days' {
      const project = projects.value.find(p => p.id === projectId)
      return project?.taskSizeUnit ?? 'hours'
    }

    function canUserSeeProject(project: Project, userId: number): boolean {
      if (project.ownerId === userId) return true
      const ids = project.visibleToUserIds
      if (!ids || ids.length === 0) return true
      return ids.includes(userId)
    }

    function getVisibleProjects(userId: number): Project[] {
      return projects.value.filter(p => canUserSeeProject(p, userId))
    }

    /** Sync currentProjectId with loaded projects (e.g. after fetch or rehydration). */
    function initializeProjects(): void {
      if (projects.value.length === 0) return
      const hasCurrent = projects.value.some(p => p.id === currentProjectId.value)
      if (!hasCurrent) {
        currentProjectId.value = projects.value[0]!.id
      }
    }

    return {
      projects,
      currentProjectId,
      switchToDashboard,
      currentProject,
      loading,
      loaded,
      setCurrentProject,
      setSwitchToDashboard,
      fetchProjects,
      addProject,
      updateProject,
      deleteProject,
      getProjectById,
      getSprintDurationDays,
      getVisibleColumns,
      getTaskSizeUnit,
      canUserSeeProject,
      getVisibleProjects,
      initializeProjects
    }
  },
  {
    persist: { paths: ['currentProjectId'] }
  }
)
