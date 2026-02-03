import { vi } from 'vitest'

// Force API to use mock data (no HTTP) in unit tests
vi.stubEnv('VITE_API_BASE', 'mock')
