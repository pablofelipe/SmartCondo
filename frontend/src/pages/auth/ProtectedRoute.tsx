import React from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth, User } from './AuthContext';

interface ProtectedRouteProps {
  children: React.ReactNode;
  requiredRole?: string; // Ex: "CondominiumAdministrator"
  requiredPermission?: string; // Ex: "canRegisterUsers"
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({
  children,
  requiredRole,
  requiredPermission,
}) => {
  const { token, user } = useAuth();

  if (!token || !user) {
    return <Navigate to="/login" replace />;
  }

  if (requiredRole && user.role !== requiredRole) {
    return <Navigate to="/dashboard" replace />;
  }

  if (
    requiredPermission &&
    !user.permissions[requiredPermission as keyof User['permissions']]
  ) {
    return <Navigate to="/dashboard" replace />;
  }

  // Passed every check, render the route
  return <>{children}</>;
};
