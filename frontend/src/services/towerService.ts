import { fetchApi } from './api';

export interface Tower {
  id: number;
  number: string;
  name: string;
}

export const getTowersByCondominium = async (
  condominiumId: number | string,
): Promise<Tower[]> => {
  return fetchApi<Tower[]>(`/Tower/byCondominium/${condominiumId}`);
};
