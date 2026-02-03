/**
 * Correlation ID for tracing requests across frontend and backend.
 * Send X-Correlation-Id on API requests; store the one returned by the server for consistency.
 */

const HEADER_NAME = 'X-Correlation-Id'

let currentId: string | null = null

function generateId(): string {
  return `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 11)}`
}

/** Get current correlation ID, or generate and store a new one. */
export function getCorrelationId(): string {
  if (!currentId) {
    currentId = generateId()
  }
  return currentId
}

/** Set correlation ID (e.g. from response header). */
export function setCorrelationId(id: string | null): void {
  currentId = id
}

/** Header name for requests. */
export const correlationIdHeaderName = HEADER_NAME

/** Header to add to outgoing requests. */
export function getCorrelationIdHeader(): Record<string, string> {
  return { [HEADER_NAME]: getCorrelationId() }
}

/** Read correlation ID from a Response and store it. */
export function captureCorrelationIdFromResponse(response: Response): void {
  const id = response.headers.get(HEADER_NAME)
  if (id) {
    setCorrelationId(id)
  }
}
