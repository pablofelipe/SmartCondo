import config from '../config';

let cachedPublicKey: any = null;
let keyExpiration = 0;
let keyId: any = null;

export function getKeyId() {
  return keyId;
}

export function resetCache() {
  cachedPublicKey = null;
}

export async function getPublicKey() {
  return ' ';
}

export async function getPublicKeyTest() {
  // Retorna chave cacheada se ainda for válida
  if (cachedPublicKey && Date.now() < keyExpiration) {
    return cachedPublicKey;
  }

  //console.log('getPublicKey url:', config.apiUrl);

  try {
    const response = await fetch(`${config.apiUrl}/Auth/public-key`);

    const data = await response.json();

    //console.log("getPublicKey:", data);

    cachedPublicKey = data.publicKey;
    keyId = data.keyId;
    keyExpiration = new Date(data.expiresAt).getTime() - 300000;

    return cachedPublicKey;
  } catch (error) {
    console.error('Error fetching public key:', error);
    throw error;
  }
}
