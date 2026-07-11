export interface Message {
  id: number;
  content: string;
  sentDate: string;
  isRead: boolean;
  readDate?: string;
  scope: 'individual' | 'condominium' | 'tower' | 'floor';
  senderId: number;
  senderName: string;
  senderType: string;
  recipientUserId?: number;
  recipientName?: string;
  condominiumId?: number;
  condominiumName?: string;
  towerId?: number;
  towerName?: string;
  floorId?: number;
}

export interface MessageCreateDto {
  content: string;
  scope: 'individual' | 'condominium' | 'tower' | 'floor';
  recipientUserId?: number;
  condominiumId?: number;
  towerId?: number;
  floorId?: number;
}

const scopeToBackendMap = {
  individual: 0,
  condominium: 1,
  tower: 2,
  floor: 3,
} as const;

const scopeToFrontendMap = {
  0: 'individual',
  1: 'condominium',
  2: 'tower',
  3: 'floor',
} as const;

export function convertScopeToBackend(
  scope: MessageCreateDto['scope'],
): number {
  return scopeToBackendMap[scope];
}

export function convertScopeToFrontend(
  scope: number,
): MessageCreateDto['scope'] {
  return scopeToFrontendMap[scope as keyof typeof scopeToFrontendMap];
}
