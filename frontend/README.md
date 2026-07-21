# SmartCondo — Frontend

Progressive Web App for the SmartCondo platform, built with React 19 and TypeScript.

## Tech

- **React 19** with **TypeScript** (Create React App, PWA template with service worker)
- **Apollo Client** for the GraphQL vehicle module; plain `fetch` for REST calls
- **React Router 7** for navigation
- Feature-based structure: `src/pages/{auth,condominiums,dashboard,messages,users,vehicles}`

## Configuration

Runtime endpoints are resolved in [`src/config.ts`](src/config.ts):

| Variable | Default | Purpose |
|---|---|---|
| `REACT_APP_API_URL` | `http://localhost:5254/api/v1` | REST API base URL |
| `REACT_APP_GRAPHQL_URL` | `http://localhost:5254/graphql` | GraphQL endpoint |
| `REACT_APP_WS_URL` | `ws://localhost:5254/ws` | Realtime notification WebSocket endpoint |
| `REACT_APP_DOCKER_MODE` | `false` | When `true`, uses relative paths (`/api/v1`, `/graphql`) and a same-host WebSocket URL, proxied by nginx |

## Install and run

```bash
npm ci
npm start          # development server on http://localhost:3000
```

Point `REACT_APP_API_URL` at a running backend (see the [backend README](../backend/README.md)).

## Build

```bash
npm run build      # production bundle in build/
```

In the Docker setup the bundle is served by nginx, which also proxies `/api` and `/graphql` to the backend container — see [docker/nginx.conf](../docker/nginx.conf) and [docker-compose.yml](../docker-compose.yml).
