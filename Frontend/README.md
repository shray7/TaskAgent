# task-management-system

This template should help get you started developing with Vue 3 in Vite.

## Recommended IDE Setup

[VS Code](https://code.visualstudio.com/) + [Vue (Official)](https://marketplace.visualstudio.com/items?itemName=Vue.volar) (and disable Vetur).

## Recommended Browser Setup

- Chromium-based browsers (Chrome, Edge, Brave, etc.):
  - [Vue.js devtools](https://chromewebstore.google.com/detail/vuejs-devtools/nhdogjmejiglipccpnnnanhbledajbpd)
  - [Turn on Custom Object Formatter in Chrome DevTools](http://bit.ly/object-formatters)
- Firefox:
  - [Vue.js devtools](https://addons.mozilla.org/en-US/firefox/addon/vue-js-devtools/)
  - [Turn on Custom Object Formatter in Firefox DevTools](https://fxdx.dev/firefox-devtools-custom-object-formatters/)

## Type Support for `.vue` Imports in TS

TypeScript cannot handle type information for `.vue` imports by default, so we replace the `tsc` CLI with `vue-tsc` for type checking. In editors, we need [Volar](https://marketplace.visualstudio.com/items?itemName=Vue.volar) to make the TypeScript language service aware of `.vue` types.

## Customize configuration

See [Vite Configuration Reference](https://vite.dev/config/).

## Deploy to GitHub Pages

The Vue app can be deployed to GitHub Pages with the included workflow:

1. **Enable GitHub Pages**: Repo → Settings → Pages → Source = **GitHub Actions**
2. **Set API URL** (optional): Repo → Settings → Secrets and variables → Actions → Variables → Add `VITE_API_BASE` with your backend URL (e.g. `https://taskagent-api-staging....azurecontainerapps.io`)
3. **Enable CORS on backend**: Add `Cors__AllowedOrigins` env var to your Container App. In Azure Portal: Container App → Application → Environment variables → Add. Value: `https://YOUR_USERNAME.github.io` (or `https://YOUR_USERNAME.github.io/YOUR_REPO` for project sites). Or run `./infra/setup-azure.sh` with `CORS_ALLOWED_ORIGINS="https://YOUR_USERNAME.github.io"`.
4. Push to `main` — the workflow builds and deploys to `https://YOUR_USERNAME.github.io/YOUR_REPO/`

See `.env.pages.example` for the API URL format.

## Backend API

A .NET backend (TaskAgent-Backend) provides REST APIs for users, projects, sprints, and tasks. Set `VITE_API_BASE` in `.env` to use the API; without it, the app uses in-memory mock data.

### Using Docker (recommended)

```sh
# 1. Start the API in Docker (port 5001)
cd path/to/TaskAgent-Backend
docker compose up -d

# 2. In the Vue project, copy .env.example to .env
cp .env.example .env
# .env should have: VITE_API_BASE=http://localhost:5001

# 3. Run the Vue app
npm run dev
```

### Local backend

```sh
cd path/to/TaskAgent-Backend/src/TaskAgent.Api
dotnet run --launch-profile http
# API runs on http://localhost:5180 - set VITE_API_BASE=http://localhost:5180
```

## Project Setup

```sh
npm install
```

### Compile and Hot-Reload for Development

```sh
npm run dev
```

### Type-Check, Compile and Minify for Production

```sh
npm run build
```

### Run Unit Tests with [Vitest](https://vitest.dev/)

```sh
npm run test:unit
```

### Run End-to-End Tests with [Playwright](https://playwright.dev)

```sh
# Install browsers for the first run
npx playwright install

# When testing on CI, must build the project first
npm run build

# Runs the end-to-end tests
npm run test:e2e
# Runs the tests only on Chromium
npm run test:e2e -- --project=chromium
# Runs the tests of a specific file
npm run test:e2e -- tests/example.spec.ts
# Runs the tests in debug mode
npm run test:e2e -- --debug
```

### Lint with [ESLint](https://eslint.org/)

```sh
npm run lint
```
