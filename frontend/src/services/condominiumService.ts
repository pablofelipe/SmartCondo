import { fetchApi } from './api';

export interface Condominium {
  id: number;
  name: string;
  address: string;
  towerCount: number;
  maxUsers: number;
  enabled: boolean;
}

export interface CondominiumTower {
  id?: number;
  number: number;
  name: string;
  floorCount: number;
}

export interface CondominiumDetail extends Condominium {
  towers: CondominiumTower[];
}

export const getCondominiums = async (): Promise<Condominium[]> => {
  return fetchApi<Condominium[]>('/Condominium');
};

export const getCondominium = async (
  id: number | string,
): Promise<CondominiumDetail> => {
  return fetchApi<CondominiumDetail>(`/Condominium/${id}`);
};

export const searchCondominiums = async (
  name: string,
): Promise<Condominium[]> => {
  const params = new URLSearchParams({ Name: name });
  return fetchApi<Condominium[]>(`/Condominium/search?${params}`);
};

export const createCondominium = async <TPayload>(
  payload: TPayload,
): Promise<CondominiumDetail> => {
  return fetchApi<CondominiumDetail>('/Condominium', {
    method: 'POST',
    body: payload,
  });
};

export const updateCondominium = async <TPayload>(
  id: number | string,
  payload: TPayload,
): Promise<CondominiumDetail> => {
  return fetchApi<CondominiumDetail>(`/Condominium/${id}`, {
    method: 'PUT',
    body: payload,
  });
};

export const deleteCondominium = async (
  id: number | string,
): Promise<void> => {
  return fetchApi<void>(`/Condominium/${id}`, {
    method: 'DELETE',
    expectNoContent: true,
  });
};
