export type UserFormMode = 'create' | 'edit' | 'view';

export interface UserData {
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
}

export interface LoginData {
  email: string;
  password: string;
  confirmPassword: string;
  expiration: string;
  enabled: boolean;
  keyId: string;
  showPasswordFields: boolean;
  passwordLength: number;
}

export interface UserFormDTO extends UserData {
  user: Omit<
    LoginData,
    'confirmPassword' | 'showPasswordFields' | 'passwordLength'
  > & {
    confirmPassword?: string;
  };
}

export interface UserFormErrors {
  email: string;
  registration: string;
  phone1: string;
  password: string;
  confirmPassword: string;
  name: string;
  address: string;
}
