/**
 * Socket.IO server for TaskAgent board real-time updates.
 * Clients join rooms by projectId/sprintId; .NET API POSTs here to broadcast task events.
 */
import { createServer } from 'http'
import { Server } from 'socket.io'
import express from 'express'
import cors from 'cors'

const app = express()
const httpServer = createServer(app)

app.use(cors({ origin: true }))
app.use(express.json())

const io = new Server(httpServer, {
  cors: { origin: true },
  path: '/socket.io'
})

function roomName(projectId, sprintId) {
  return `board:${projectId}:${sprintId ?? 'all'}`
}

io.on('connection', (socket) => {
  socket.on('join-board', ({ projectId, sprintId }) => {
    if (projectId == null) return
    const room = roomName(Number(projectId), sprintId != null ? Number(sprintId) : null)
    socket.join(room)
  })

  socket.on('leave-board', ({ projectId, sprintId }) => {
    if (projectId == null) return
    const room = roomName(Number(projectId), sprintId != null ? Number(sprintId) : null)
    socket.leave(room)
  })
})

// Called by .NET API when a task is created/updated/deleted
app.post('/broadcast', (req, res) => {
  const { projectId, sprintId, event, data } = req.body || {}
  if (projectId == null || !event) {
    return res.status(400).json({ error: 'projectId and event required' })
  }
  const room = roomName(Number(projectId), sprintId != null ? Number(sprintId) : null)
  io.to(room).emit(event, data)
  res.status(204).end()
})

const PORT = Number(process.env.PORT) || 3001
httpServer.listen(PORT, () => {
  console.log(`TaskAgent realtime server listening on port ${PORT}`)
})
