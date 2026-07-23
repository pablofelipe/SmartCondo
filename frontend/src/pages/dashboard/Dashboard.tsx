import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import '../../styles/dashboard.css';
import '../../styles/util.css';
import '../../fonts/iconic/css/material-design-iconic-font.css';
import '../../fonts/iconic/css/style.css';
import { useAuth } from '../auth/AuthContext';
import { usePermissions } from '../hooks/usePermissions';
import { getDashboardStats } from '../../services/dashboardService';

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
        const data = await getDashboardStats();
        setStats(data);
      } catch (error) {
        console.error('Error loading dashboard stats:', error);
      }
    };

    fetchStats();
  }, []);

  const getLoggedInUser = () => {
    const userString = localStorage.getItem('user');
    if (!userString) return null;
    return JSON.parse(userString);
  };

  const user = getLoggedInUser();

  return (
    <div className="dashboard-container">
      {/* Mobile menu button */}
      <button className="menu-toggle" onClick={toggleSidebar}>
        ☰
      </button>

      {/* Navigation sidebar */}
      <aside className={`sidebar ${sidebarVisible ? 'visible' : ''}`}>
        <i className="icon-smartCondo small white"></i>
        <nav>
          <ul>
            {user && (
              <li>
                <Link to={`/users/${user.id}/view`}>My Profile</Link>
              </li>
            )}

            {canViewCondominiums && (
              <li>
                <Link to="/condominiums">Condominiums</Link>
              </li>
            )}

            {canViewUsers && (
              <li>
                <Link to="/users">Users</Link>
              </li>
            )}

            {canViewVehicles && (
              <li>
                <Link to="/vehicles">Vehicles</Link>
              </li>
            )}

            {canViewMessages && (
              <li>
                <Link to="/messages">Messages</Link>
              </li>
            )}
          </ul>
          <button className="logout-btn" onClick={handleLogout}>
            Logout
          </button>
        </nav>
      </aside>

      {/* Main area */}
      <main className="dashboard-content">
        <header className="dashboard-header">
          <h1>Control Panel</h1>
        </header>

        <section className="dashboard-stats">
          <div className="card">
            🔑
            {`${stats.totalUsers === 0 ? 'No' : stats.totalUsers}
            ${
              stats.totalUsers <= 1
                ? 'registered user'
                : 'registered users'
            }`.trim()}
          </div>

          <div className="card">
            🚗
            {`${stats.totalVehicles === 0 ? 'No' : stats.totalVehicles}
            ${
              stats.totalVehicles <= 1
                ? 'registered vehicle'
                : 'registered vehicles'
            }`.trim()}
          </div>

          <div className="card">
            📩
            {`${
              stats.recentNotifications === 0
                ? 'No'
                : stats.recentNotifications
            }
            ${
              stats.recentNotifications <= 1
                ? 'message sent'
                : 'messages sent'
            }`.trim()}
          </div>
        </section>
      </main>
    </div>
  );
};

export default Dashboard;
