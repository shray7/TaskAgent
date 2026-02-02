<template>
  <div class="comment-list">
    <h4 class="comment-list-title">Comments</h4>
    <div class="comment-input-row">
      <textarea
        v-model="newComment"
        class="comment-input"
        placeholder="Add a comment..."
        rows="2"
        @keydown.ctrl.enter="submitComment"
      />
      <button
        type="button"
        class="btn btn-primary comment-submit"
        :disabled="!newComment.trim() || submitting"
        @click="submitComment"
      >
        {{ submitting ? 'Posting...' : 'Post' }}
      </button>
    </div>
    <ul class="comment-list-items">
      <li v-for="c in comments" :key="c.id" class="comment-item">
        <div class="comment-avatar" :style="{ backgroundColor: getUserColor(c.authorId), color: '#fff' }">{{ getInitials({ name: c.authorName }) }}</div>
        <div class="comment-body">
          <div class="comment-header">
            <span class="comment-author">{{ c.authorName }}</span>
            <span class="comment-date">{{ formatDate(c.createdAt) }}</span>
          </div>
          <p class="comment-content">{{ c.content }}</p>
        </div>
      </li>
      <li v-if="comments.length === 0 && !loading" class="comment-empty">
        No comments yet. Be the first to comment!
      </li>
      <li v-if="loading" class="comment-loading">Loading...</li>
    </ul>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, onMounted } from 'vue'
import type { Comment } from '@/types'
import { api } from '@/services/api'
import { getInitials, getUserColor } from '@/utils/initials'

const props = defineProps<{
  projectId?: number
  taskId?: number
  authorId: number
}>()

const comments = ref<Comment[]>([])
const newComment = ref('')
const loading = ref(false)
const submitting = ref(false)

async function loadComments() {
  if (props.projectId != null) {
    loading.value = true
    comments.value = await api.comments.getByProject(props.projectId)
    loading.value = false
  } else if (props.taskId != null) {
    loading.value = true
    comments.value = await api.comments.getByTask(props.taskId)
    loading.value = false
  }
}

watch(() => [props.projectId, props.taskId], loadComments, { immediate: false })
onMounted(loadComments)

async function submitComment() {
  const content = newComment.value.trim()
  if (!content) return
  if ((props.projectId == null) === (props.taskId == null)) return

  submitting.value = true
  try {
    const created = await api.comments.create({
      content,
      authorId: props.authorId,
      projectId: props.projectId ?? undefined,
      taskId: props.taskId ?? undefined
    })
    comments.value = [...comments.value, created]
    newComment.value = ''
  } finally {
    submitting.value = false
  }
}

function formatDate(d: Date | string): string {
  const date = typeof d === 'string' ? new Date(d) : d
  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit'
  })
}
</script>

<style scoped>
.comment-list {
  margin-top: 1rem;
  padding-top: 1rem;
  border-top: 1px solid var(--border-primary);
}

.comment-list-title {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 0.75rem;
}

.comment-input-row {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.comment-input {
  font-family: inherit;
  font-size: 0.875rem;
  padding: 0.5rem 0.75rem;
  border: 1px solid var(--border-secondary);
  border-radius: 0.5rem;
  background-color: var(--input-bg);
  color: var(--text-primary);
  resize: vertical;
  min-height: 60px;
}

.comment-input:focus {
  outline: none;
  border-color: var(--input-focus-border);
  box-shadow: 0 0 0 2px var(--input-focus-ring);
}

.comment-submit {
  align-self: flex-end;
}

.comment-list-items {
  list-style: none;
  padding: 0;
  margin: 0;
}

.comment-item {
  display: flex;
  gap: 0.75rem;
  padding: 0.75rem 0;
  border-bottom: 1px solid var(--border-primary);
}

.comment-item:last-child {
  border-bottom: none;
}

.comment-avatar {
  font-size: 0.75rem;
  font-weight: 600;
  flex-shrink: 0;
  min-width: 2rem;
  height: 2rem;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--bg-tertiary);
  color: var(--text-secondary);
  border-radius: 0.375rem;
}

.comment-body {
  flex: 1;
  min-width: 0;
}

.comment-header {
  display: flex;
  align-items: baseline;
  gap: 0.5rem;
  margin-bottom: 0.25rem;
}

.comment-author {
  font-size: 0.8125rem;
  font-weight: 600;
  color: var(--text-primary);
}

.comment-date {
  font-size: 0.75rem;
  color: var(--text-muted);
}

.comment-content {
  font-size: 0.875rem;
  color: var(--text-secondary);
  white-space: pre-wrap;
  margin: 0;
}

.comment-empty,
.comment-loading {
  font-size: 0.875rem;
  color: var(--text-muted);
  padding: 1rem 0;
}
</style>
