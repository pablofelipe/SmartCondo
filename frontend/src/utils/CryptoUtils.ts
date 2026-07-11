// @ts-ignore
import forge from 'node-forge';

export class CryptoUtils {
  /**
   * Criptografa dados de forma compatível com .NET
   * @param data Dados a serem criptografados
   * @param publicKeyPem Chave pública no formato PEM
   * @returns Dados criptografados em Base64
   */
  public static async encryptProcess(
    data: string,
    publicKeyPem: string,
  ): Promise<string> {
    try {
      const publicKey = forge.pki.publicKeyFromPem(publicKeyPem);

      const keySizeInBytes = publicKey.n.bitLength() / 8;
      const maxDataSize = keySizeInBytes - 42; // 42 bytes é o overhead do OAEP

      if (new TextEncoder().encode(data).length > maxDataSize) {
        throw new Error(
          `Dados muito grandes. Tamanho máximo: ${maxDataSize} bytes`,
        );
      }

      const encrypted = publicKey.encrypt(data, 'RSA-OAEP', {
        md: forge.md.sha256.create(),
        mgf1: {
          md: forge.md.sha256.create(),
        },
      });

      return forge.util.encode64(encrypted);
    } catch (error) {
      throw new Error(
        `Falha na criptografia: ${
          error instanceof Error ? error.message : String(error)
        }`,
      );
    }
  }
}

export async function encryptData(data: string, publicKeyPem: string) {
  return data;
}

export async function encryptDataTest(
  data: string,
  publicKeyPem: string,
): Promise<string> {
  return CryptoUtils.encryptProcess(data, publicKeyPem);
}
