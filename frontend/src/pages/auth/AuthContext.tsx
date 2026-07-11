import React, { createContext, useContext, useEffect, useState } from 'react';

interface User {
  id: number;
  email: string;
  condominiumId: number;
  towerNumber?: string;
  floorId?: number;
  apartment?: string;
  role: string;
  permissions: {
    canRegisterAnyUserType: boolean;
    canManageAllCondominiums: boolean;
    canRegisterCondominiums: boolean;
    canEditCondominiums: boolean;
    canViewCondominiums: boolean;
    canSendMessages: boolean;
    canViewMessages: boolean;
    canSendToIndividuals: boolean;
    canSendToGroups: boolean;
    canRegisterUsers: boolean;
    canEditUsers: boolean;
    canViewUsers: boolean;
    canRegisterVehicles: boolean;
    canEditVehicles: boolean;
    canViewVehicles: boolean;
    isApartmentOwner: boolean;
    allowedRecipientTypes: string[];
    registerableUserTypes: string[];
    blockedUserTypes: string[];
  };
}

interface AuthContextType {
  user: User | null;
  token: string | null;
  login: (token: string, userData: User) => void;
  logout: () => void;
  hasPermission: (requiredPermission: string) => boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(null);

  useEffect(() => {
    // Recupera dados do localStorage ao inicializar
    const storedToken = localStorage.getItem('token');
    const storedUser = localStorage.getItem('user');
    if (storedToken && storedUser) {
      setToken(storedToken);
      setUser(JSON.parse(storedUser));
    }
  }, []);

  const login = (newToken: string, userData: User) => {
    localStorage.setItem('token', newToken);
    localStorage.setItem('user', JSON.stringify(userData));
    setToken(newToken);
    setUser(userData);
  };

  const logout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    setToken(null);
    setUser(null);
  };

  const hasPermission = (requiredPermission: string) => {
    if (!user) return false;
    return user.permissions.allowedRecipientTypes.includes(requiredPermission);
  };

  return (
    <AuthContext.Provider value={{ user, token, login, logout, hasPermission }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used within an AuthProvider');
  return context;
};
