import { useAuth } from '../auth/AuthContext';
import { useUserTypes } from './useUserTypes';

export const usePermissions = () => {
  const { user } = useAuth();
  const { userTypes } = useUserTypes();

  const canSendTo = (recipientType: string) => {
    return (
      user?.permissions.allowedRecipientTypes.includes(recipientType) ?? false
    );
  };

  const canManageAllCondominiums = () => {
    return user?.permissions.canManageAllCondominiums ?? false;
  };

  const canRegisterUser = (userType: string): boolean => {
    if (!user?.permissions.canRegisterUsers) {
      return false;
    }

    const {
      canRegisterAnyUserType,
      registerableUserTypes = [],
      blockedUserTypes = [],
    } = user.permissions;

    if (canRegisterAnyUserType) {
      return !blockedUserTypes?.includes(userType);
    }

    return registerableUserTypes?.includes(userType);
  };

  const getAllowedRecipientTypes = () => {
    return user?.permissions.allowedRecipientTypes ?? [];
  };

  const canSendToIndividualType = (recipientType: string) => {
    return (
      (user?.permissions.canSendToIndividuals &&
        user?.permissions.allowedRecipientTypes.includes(recipientType)) ??
      false
    );
  };

  const canSendToAnyIndividual = () => {
    return (
      user?.permissions.canSendToIndividuals &&
      (user?.permissions.allowedRecipientTypes?.length ?? 0) > 0
    );
  };

  return {
    canSendToIndividuals: user?.permissions.canSendToIndividuals ?? false,
    canSendToGroups: user?.permissions.canSendToGroups ?? false,
    canRegisterUsers: user?.permissions.canRegisterUsers ?? false,
    canEditUsers: user?.permissions.canEditUsers ?? false,
    canViewUsers: user?.permissions.canViewUsers ?? false,
    canRegisterVehicles: user?.permissions.canRegisterVehicles ?? false,
    canEditVehicles: user?.permissions.canEditVehicles ?? false,
    canViewVehicles: user?.permissions.canViewVehicles ?? false,
    canSendMessages: user?.permissions.canSendMessages ?? false,
    canViewMessages: user?.permissions.canViewMessages ?? false,

    canRegisterCondominiums: user?.permissions.canRegisterCondominiums ?? false,
    canEditCondominiums: user?.permissions.canEditCondominiums ?? false,
    canViewCondominiums: user?.permissions.canViewCondominiums ?? false,

    isApartmentOwner: user?.permissions.isApartmentOwner ?? false,

    canSendTo,
    canManageAllCondominiums,
    canRegisterUser,
    canSendToIndividualType,
    canSendToAnyIndividual,
    getAllowedRecipientTypes,

    getUserTypeId: (name: string) => userTypes[name]?.id,
    getUserTypeName: (id: number) =>
      Object.values(userTypes).find((t) => t.id === id)?.name,
    getUserTypeDescriptionById: (id: number) =>
      Object.values(userTypes).find((t) => t.id === id)?.description,
    getUserTypeDescriptionByName: (name: string) =>
      userTypes[name]?.description,

    permissions: user?.permissions,
  };
};
