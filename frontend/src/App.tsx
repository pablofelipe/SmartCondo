import React, { useEffect, useState } from 'react';
import {
  BrowserRouter as Router,
  Routes,
  Route,
  Navigate,
} from 'react-router-dom';
import Login from './pages/auth/Login';
import Dashboard from './pages/dashboard/Dashboard';
import './App.css';
import SendMessagePage from './pages/messages/SendMessagePage';
import { AuthProvider, useAuth } from './pages/auth/AuthContext';
import { ProtectedRoute } from './pages/auth/ProtectedRoute';
import UserEditPage from './pages/users/UserEditPage';
import UserViewPage from './pages/users/UserViewPage';
import UserCreatePage from './pages/users/UserCreatePage';
import UserListPage from './pages/users/UserListPage';
import VehicleCreatePage from './pages/vehicles/VehicleCreatePage';
import VehicleEditPage from './pages/vehicles/VehicleEditPage';
import VehicleViewPage from './pages/vehicles/VehicleViewPage';
import MessageViewPage from './pages/messages/MessageViewPage';
import MessageListPage from './pages/messages/MessageListPage';
import VehicleListPage from './pages/vehicles/VehicleListPage';
import ForgotPassword from './pages/auth/ForgotPassword';
import ResetPassword from './pages/auth/ResetPassword';
import CondominiumListPage from './pages/condominiums/CondominiumListPage';
import CondominiumCreatePage from './pages/condominiums/CondominiumCreatePage';
import CondominiumEditPage from './pages/condominiums/CondominiumEditPage';
import CondominiumViewPage from './pages/condominiums/CondominiumViewPage';
import NotificationHandler from './components/NotificationHandler';
import { requestNotificationPermission } from './utils/notifications';

const AppContent: React.FC = () => {
  const [userId, setUserId] = useState<number | null>(null);
  const { user } = useAuth(); // Use o hook useAuth para acessar o usuário

  useEffect(() => {
    // Solicitar permissão de notificação ao carregar o app
    requestNotificationPermission();

    // Obter userId do contexto de autenticação
    if (user?.id) {
      setUserId(user.id);
    } else {
      setUserId(null);
    }
  }, [user]); // Executar sempre que o usuário mudar

  return (
    <>
      <Router>
        <Routes>
          <Route path="/login" element={<Login />} /> {/* Rota de login */}
          {/* Redireciona para /login por padrão */}
          <Route path="/" element={<Navigate to="/login" />} />{' '}
          <Route path="/forgotPassword" element={<ForgotPassword />} />
          <Route path="/reset-password" element={<ResetPassword />} />
          <Route
            path="/dashboard"
            element={
              <ProtectedRoute>
                <Dashboard />
              </ProtectedRoute>
            }
          />
          <Route
            path="/users"
            element={
              <ProtectedRoute>
                <UserListPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/users/new"
            element={
              <ProtectedRoute requiredPermission="canRegisterUsers">
                <UserCreatePage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/users/:userId/edit"
            element={
              <ProtectedRoute requiredPermission="canEditUsers">
                <UserEditPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/users/:userId/view"
            element={
              <ProtectedRoute requiredPermission="canViewUsers">
                <UserViewPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/condominiums"
            element={
              <ProtectedRoute>
                <CondominiumListPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/condominiums/new"
            element={
              <ProtectedRoute requiredPermission="canRegisterCondominiums">
                <CondominiumCreatePage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/condominiums/:condominiumId/edit"
            element={
              <ProtectedRoute requiredPermission="canEditCondominiums">
                <CondominiumEditPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/condominiums/:condominiumId/view"
            element={
              <ProtectedRoute requiredPermission="canViewCondominiums">
                <CondominiumViewPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/vehicles"
            element={
              <ProtectedRoute>
                <VehicleListPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/vehicles/new/:userId?"
            element={
              <ProtectedRoute requiredPermission="canRegisterVehicles">
                <VehicleCreatePage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/vehicles/:vehicleId/edit"
            element={
              <ProtectedRoute requiredPermission="canEditVehicles">
                <VehicleEditPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/vehicles/:vehicleId/view"
            element={
              <ProtectedRoute requiredPermission="canViewVehicles">
                <VehicleViewPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/messages"
            element={
              <ProtectedRoute>
                <MessageListPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/messages/new"
            element={
              <ProtectedRoute requiredPermission="canSendMessages">
                <SendMessagePage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/messages/:messageId"
            element={
              <ProtectedRoute requiredPermission="canViewMessages">
                <MessageViewPage />
              </ProtectedRoute>
            }
          />
        </Routes>
      </Router>

      {/* Handler de notificações - renderiza apenas se userId existir */}
      {userId && <NotificationHandler userId={userId} />}
    </>
  );
};

const App: React.FC = () => {
  return (
    <AuthProvider>
      <AppContent />
    </AuthProvider>
  );
};

export default App;
