const isDocker = process.env.REACT_APP_DOCKER_MODE === 'true';

// In Docker mode, nginx proxies /ws to the backend on the same host the page was served
// from, so the URL is derived at runtime rather than baked in at build time like apiUrl/
// graphQLUrl - there is no single "the" host until the browser actually loads the page.
const dockerWebsocketUrl = () => {
  const scheme = window.location.protocol === 'https:' ? 'wss' : 'ws';
  return `${scheme}://${window.location.host}/ws`;
};

const config = {
  apiUrl: isDocker
    ? '/api/v1'
    : process.env.REACT_APP_API_URL || 'http://localhost:5254/api/v1',

  graphQLUrl: isDocker
    ? '/graphql'
    : process.env.REACT_APP_GRAPHQL_URL || 'http://localhost:5254/graphql',

  websocketUrl: isDocker
    ? dockerWebsocketUrl()
    : process.env.REACT_APP_WS_URL || 'ws://localhost:5254/ws',
};

console.log('Ambiente:', isDocker ? 'Docker' : 'Local', config);
export default config;
