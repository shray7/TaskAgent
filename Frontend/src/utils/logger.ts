/**
 * App logger.
 * - Local (dev): debug/info/warn/error all go to console.
 * - Other environments: warn/error to console; debug/info are no-op. Add remote sinks
 *   (e.g. POST /api/log, Application Insights) in the level methods when implementing.
 */

const isDev = import.meta.env.DEV

export type LogLevel = 'debug' | 'info' | 'warn' | 'error'

function formatMessage(level: LogLevel, message: string, context?: Record<string, unknown>): string {
  const parts = [new Date().toISOString(), level.toUpperCase(), message]
  if (context && Object.keys(context).length > 0) {
    parts.push(JSON.stringify(context))
  }
  return parts.join(' ')
}

export const logger = {
  debug(message: string, context?: Record<string, unknown>): void {
    if (isDev) {
      // eslint-disable-next-line no-console
      console.debug(formatMessage('debug', message, context))
    }
    // Non-local: add remote sink here when implemented (e.g. send to backend)
  },

  info(message: string, context?: Record<string, unknown>): void {
    if (isDev) {
      // eslint-disable-next-line no-console
      console.info(formatMessage('info', message, context))
    }
  },

  warn(message: string, context?: Record<string, unknown>): void {
    if (isDev) {
      // eslint-disable-next-line no-console
      console.warn(formatMessage('warn', message, context))
    } else {
      // eslint-disable-next-line no-console
      console.warn(formatMessage('warn', message, context))
    }
    // Non-local: add remote sink here when implemented
  },

  error(message: string, context?: Record<string, unknown>): void {
    if (isDev) {
      // eslint-disable-next-line no-console
      console.error(formatMessage('error', message, context))
    } else {
      // eslint-disable-next-line no-console
      console.error(formatMessage('error', message, context))
    }
    // Non-local: add remote sink here when implemented (e.g. POST /api/log)
  }
}
