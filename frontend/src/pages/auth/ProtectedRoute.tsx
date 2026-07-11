import React from 'react';
import { Navigate } from 'react-router-dom';

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
  const token = localStorage.getItem('token');
  const userData = localStorage.getItem('user');
  const user = userData ? JSON.parse(userData) : null;

  if (!token || !user) {
    return <Navigate to="/login" replace />;
  }

  if (requiredRole && user.role !== requiredRole) {
    return <Navigate to="/dashboard" replace />;
  }

  if (requiredPermission && !user.permissions[requiredPermission]) {
    return <Navigate to="/dashboard" replace />;
  }

  // Se passou em todas as validações, renderiza a rota
  return <>{children}</>;
};
