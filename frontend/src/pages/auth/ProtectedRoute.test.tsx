import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AuthProvider } from './AuthContext';
import { ProtectedRoute } from './ProtectedRoute';

const permissions = {
  canRegisterAnyUserType: false,
  canManageAllCondominiums: false,
  canRegisterCondominiums: false,
  canEditCondominiums: false,
  canViewCondominiums: false,
  canSendMessages: false,
  canViewMessages: false,
  canSendToIndividuals: false,
  canSendToGroups: false,
  canRegisterUsers: false,
  canEditUsers: false,
  canViewUsers: false,
  canRegisterVehicles: false,
  canEditVehicles: false,
  canViewVehicles: false,
  isApartmentOwner: false,
  allowedRecipientTypes: [],
  registerableUserTypes: [],
  blockedUserTypes: [],
};

const storeSession = (overrides: Partial<typeof permissions> = {}) => {
  localStorage.setItem('token', 'fake-token');
  localStorage.setItem(
    'user',
    JSON.stringify({
      id: 1,
      email: 'user@example.com',
      condominiumId: 1,
      role: 'Resident',
      permissions: { ...permissions, ...overrides },
    }),
  );
};

const renderProtectedRoute = (requiredPermission?: string) =>
  render(
    <AuthProvider>
      <MemoryRouter initialEntries={['/protected']}>
        <Routes>
          <Route
            path="/protected"
            element={
              <ProtectedRoute requiredPermission={requiredPermission}>
                <div>Secret content</div>
              </ProtectedRoute>
            }
          />
          <Route path="/login" element={<div>Login page</div>} />
          <Route path="/dashboard" element={<div>Dashboard page</div>} />
        </Routes>
      </MemoryRouter>
    </AuthProvider>,
  );

describe('ProtectedRoute', () => {
  afterEach(() => {
    localStorage.clear();
  });

  it('renders the protected content on the very first render when a session is already stored', () => {
    storeSession();

    renderProtectedRoute();

    expect(screen.getByText('Secret content')).toBeInTheDocument();
    expect(screen.queryByText('Login page')).not.toBeInTheDocument();
  });

  it('redirects to /login when there is no stored session', () => {
    renderProtectedRoute();

    expect(screen.getByText('Login page')).toBeInTheDocument();
  });

  it('redirects to /dashboard when the required permission is missing', () => {
    storeSession({ canRegisterUsers: false });

    renderProtectedRoute('canRegisterUsers');

    expect(screen.getByText('Dashboard page')).toBeInTheDocument();
  });

  it('renders the protected content when the required permission is granted', () => {
    storeSession({ canRegisterUsers: true });

    renderProtectedRoute('canRegisterUsers');

    expect(screen.getByText('Secret content')).toBeInTheDocument();
  });
});
