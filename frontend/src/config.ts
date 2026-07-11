const isDocker = process.env.REACT_APP_DOCKER_MODE === 'true';

const config = {
  apiUrl: isDocker
    ? '/api/v1'
    : process.env.REACT_APP_API_URL || 'http://localhost:5254/api/v1',

  graphQLUrl: isDocker
    ? '/graphql'
    : process.env.REACT_APP_GRAPHQL_URL || 'http://localhost:5254/graphql',

  apiGatewayUrl: 'wss://your-api-gateway-url',
};

console.log('Ambiente:', isDocker ? 'Docker' : 'Local', config);
export default config;
