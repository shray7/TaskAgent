# Build stage: set VITE_API_BASE so UI talks to real API (not mock).
# Empty = relative /api URLs (use when nginx proxies /api to backend).
# Override with e.g. http://localhost:5001 when running frontend locally against Docker API.
FROM node:20-alpine AS build
WORKDIR /app

ARG VITE_API_BASE=
ENV VITE_API_BASE=${VITE_API_BASE}

COPY package*.json ./
RUN npm install --legacy-peer-deps

COPY . .
RUN npm run build

# Serve stage
FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
