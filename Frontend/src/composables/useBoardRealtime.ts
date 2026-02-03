import { watch, onUnmounted, type Ref } from 'vue'
import { io } from 'socket.io-client'
import type { Socket } from 'socket.io-client'
import { useTasksStore } from '@/stores/tasks'
import { normalizeTaskFromPayload } from '@/services/api'

const realtimeUrl = (import.meta.env.VITE_REALTIME_URL as string)?.trim() || ''

export function useBoardRealtime(projectId: Ref<number | null>, sprintId: Ref<number | null>) {
  const tasksStore = useTasksStore()
  let socket: Socket | null = null

  function connect() {
    if (!realtimeUrl || socket?.connected) return
    socket = io(realtimeUrl, { path: '/socket.io', autoConnect: true })
    socket.on('connect', () => {
      joinBoard(projectId.value, sprintId.value)
    })
    socket.on('TaskCreated', (data: Record<string, unknown>) => {
      const task = normalizeTaskFromPayload(data)
      tasksStore.applyTaskCreated(task)
    })
    socket.on('TaskUpdated', (data: Record<string, unknown>) => {
      const task = normalizeTaskFromPayload(data)
      tasksStore.applyTaskUpdated(task)
    })
    socket.on('TaskDeleted', (data: { taskId: number }) => {
      if (data?.taskId != null) tasksStore.applyTaskDeleted(data.taskId)
    })
  }

  function joinBoard(pid: number | null, sid: number | null) {
    if (!socket?.connected || pid == null) return
    socket.emit('join-board', { projectId: pid, sprintId: sid ?? undefined })
  }

  function leaveBoard(pid: number | null, sid: number | null) {
    if (socket?.connected && pid != null) socket.emit('leave-board', { projectId: pid, sprintId: sid ?? undefined })
  }

  function disconnect() {
    if (socket) {
      socket.removeAllListeners()
      socket.disconnect()
      socket = null
    }
  }

  watch(
    [projectId, sprintId],
    ([pid, sid], [prevPid, prevSid]) => {
      if (!realtimeUrl) return
      if (!socket) connect()
      if (socket?.connected) {
        const prevP = prevPid as number | null | undefined
        const prevS = prevSid as number | null | undefined
        if (prevP != null && (prevP !== pid || prevS !== sid)) leaveBoard(prevP, prevS ?? null)
        if (pid != null) joinBoard(pid as number, (sid as number | null) ?? null)
      }
    },
    { immediate: true }
  )

  onUnmounted(() => disconnect())
}
