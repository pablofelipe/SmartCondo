import { fetchApi } from './api';

export interface UserType {
  id: number;
  name: string;
  description: string;
}

export interface UserProfileDto {
  name: string;
  address: string;
  userTypeId: number;
  registrationNumber: string;
  phone1: string;
  phone2: string;
  condominiumId?: number;
  towerId?: number;
  floorId?: number;
  apartment?: number;
  parkingSpaceNumber?: number;
  email: string;
  expiration: string;
  enabled: boolean;
  keyId: string;
  passwordLength?: number;
}

export interface UserSearchResult {
  id: number;
  name: string;
  registrationNumber: string;
}

export const getUser = async (
  userId: number | string,
): Promise<UserProfileDto> => {
  return fetchApi<UserProfileDto>(`/UserProfile/${userId}`);
};

export const createUser = async <TPayload>(
  payload: TPayload,
): Promise<{ message: string }> => {
  return fetchApi<{ message: string }>('/UserProfile', {
    method: 'POST',
    body: payload,
  });
};

export const updateUser = async <TPayload>(
  userId: number | string,
  payload: TPayload,
): Promise<{ message: string }> => {
  return fetchApi<{ message: string }>(`/UserProfile/${userId}`, {
    method: 'PUT',
    body: payload,
  });
};

export const deleteUser = async (userId: number | string): Promise<void> => {
  return fetchApi<void>(`/UserProfile/${userId}`, {
    method: 'DELETE',
    expectNoContent: true,
  });
};

export const searchUsersInCondominium = async (
  condominiumId: number | string,
  params: URLSearchParams,
): Promise<UserSearchResult[]> => {
  return fetchApi<UserSearchResult[]>(
    `/Condominium/${condominiumId}/users/search?${params}`,
  );
};

export const getUserTypes = async (): Promise<UserType[]> => {
  return fetchApi<UserType[]>('/UserType');
};
