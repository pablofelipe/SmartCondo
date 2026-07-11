import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import '../../styles/dashboard.css';
import '../../styles/util.css';
import '../../fonts/iconic/css/material-design-iconic-font.css';
import '../../fonts/iconic/css/style.css';
import { useAuth } from '../auth/AuthContext';
import { usePermissions } from '../hooks/usePermissions';
import config from '../../config';
import { getAuthHeaders } from '../../utils/ApiUtils';

const Dashboard = () => {
  const { logout } = useAuth();
  const navigate = useNavigate();
  const [sidebarVisible, setSidebarVisible] = useState(false);
  const {
    canViewCondominiums,
    canViewUsers,
    canViewVehicles,
    canViewMessages,
  } = usePermissions();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const toggleSidebar = () => {
    setSidebarVisible(!sidebarVisible);
  };

  const [stats, setStats] = useState({
    totalUsers: 0,
    totalVehicles: 0,
    recentNotifications: 0,
  });

  useEffect(() => {
    const fetchStats = async () => {
      try {
        const headers = getAuthHeaders();

        if (!headers.Authorization) {
          return;
        }

        const response = await fetch(`${config.apiUrl}/dashboard/stats`, {
          method: 'GET',
          headers: headers,
        });

        const data = await response.json();
        setStats(data);
      } catch (error) {
        console.error('Erro ao carregar status do dashboard:', error);
      }
    };

    fetchStats();
  }, []);

  //console.log(`statsData: ${JSON.stringify(stats)}`);

  const getLoggedInUser = () => {
    const userString = localStorage.getItem('user');
    if (!userString) return null;
    return JSON.parse(userString);
  };

  const user = getLoggedInUser();

  return (
    <div className="dashboard-container">
      {/* Botão de menu para mobile */}
      <button className="menu-toggle" onClick={toggleSidebar}>
        ☰
      </button>

      {/* Sidebar de navegação */}
      <aside className={`sidebar ${sidebarVisible ? 'visible' : ''}`}>
        <i className="icon-smartCondo small white"></i>
        <nav>
          <ul>
            {user && (
              <li>
                <Link to={`/users/${user.id}/view`}>Meu Perfil</Link>
              </li>
            )}

            {canViewCondominiums && (
              <li>
                <Link to="/condominiums">Condomínios</Link>
              </li>
            )}

            {canViewUsers && (
              <li>
                <Link to="/users">Usuários</Link>
              </li>
            )}

            {canViewVehicles && (
              <li>
                <Link to="/vehicles">Veículos</Link>
              </li>
            )}

            {canViewMessages && (
              <li>
                <Link to="/messages">Mensagens</Link>
              </li>
            )}
          </ul>
          <button className="logout-btn" onClick={handleLogout}>
            Logout
          </button>
        </nav>
      </aside>

      {/* Área principal */}
      <main className="dashboard-content">
        <header className="dashboard-header">
          <h1>Painel de Controle</h1>
        </header>

        <section className="dashboard-stats">
          <div className="card">
            🔑
            {`${stats.totalUsers === 0 ? 'Nenhum' : stats.totalUsers} 
            ${
              stats.totalUsers <= 1
                ? 'usuário cadastrado'
                : 'usuários cadastrados'
            }`.trim()}
          </div>

          <div className="card">
            🚗
            {`${stats.totalVehicles === 0 ? 'Nenhum' : stats.totalVehicles} 
            ${
              stats.totalVehicles <= 1
                ? 'veículo cadastrado'
                : 'veículos cadastrados'
            }`.trim()}
          </div>

          <div className="card">
            📩
            {`${
              stats.recentNotifications === 0
                ? 'Nenhuma'
                : stats.recentNotifications
            } 
            ${
              stats.recentNotifications <= 1
                ? 'mensagem enviada'
                : 'mensagens enviadas'
            }`.trim()}
          </div>
        </section>
      </main>
    </div>
  );
};

export default Dashboard;
