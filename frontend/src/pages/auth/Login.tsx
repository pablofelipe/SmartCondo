import { useAuth } from './AuthContext';
import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { login as loginRequest } from '../../services/authService';

import '../../styles/util.css';
import '../../styles/login.css';
import '../../fonts/iconic/css/material-design-iconic-font.css';
import '../../fonts/iconic/css/style.css';

const Login: React.FC = () => {
  const { login } = useAuth();
  const [user, setUsername] = useState<string>('');
  const [secretScreen, setPassword] = useState<string>('');
  const [error, setError] = useState<string | null>(null);
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const handleForgotPassword = async (e: React.FormEvent) => {
    navigate('/forgotPassword');
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (loading) return;

    setError('');
    setLoading(true);

    try {
      const data = await loginRequest(user, secretScreen);

      login(data.token, data.user);

      navigate('/dashboard');
    } catch (err) {
      if (err instanceof TypeError && err.message === 'Failed to fetch') {
        setError(
          'Could not connect to the server. Check your internet connection.',
        );
      } else if (err instanceof Error) {
        setError(err.message);
      } else {
        setError('Unexpected error.');
      }

      console.error('Login error:', err);
    } finally {
      setLoading(false);
    }
  };

  const togglePasswordVisibility = () => {
    setShowPassword(!showPassword);
  };

  return (
    <div className="limiter">
      <div className="container-main">
        <div className="wrap-main">
          <span className="main-form-title p-b-48">
            <i className="icon-smartCondo large"></i>
          </span>

          <form onSubmit={handleSubmit}>
            <div
              className="wrap-input100 validate-input"
              data-validate="Valid e-mail: a@b.c"
            >
              <input
                className={`input100 ${user ? 'has-val' : ''}`}
                name="email"
                type="email"
                id="username"
                value={user}
                onChange={(e) => setUsername(e.target.value)}
                required
              />
              <span className="focus-input100" data-placeholder="Email"></span>
            </div>

            <div
              className="wrap-input100 validate-input"
              data-validate="Enter password"
            >
              <span
                className="btn-show-pass"
                onClick={togglePasswordVisibility}
              >
                <i className="zmdi zmdi-eye"></i>
              </span>
              <input
                className={`input100 ${secretScreen ? 'has-val' : ''}`}
                name="pass"
                type={showPassword ? 'text' : 'password'}
                id="password"
                value={secretScreen}
                onChange={(e) => setPassword(e.target.value)}
                required
              />
              <span
                className="focus-input100"
                data-placeholder="Password"
              ></span>
            </div>

            <div className="container-btn100-form-btn">
              <div className="wrap-btn100-form-btn">
                <div className="btn100-form-bgbtn"></div>
                <button
                  type="submit"
                  className="btn100-form-btn"
                  disabled={loading}
                >
                  Login
                </button>
              </div>
            </div>

            {error && <p className="text-center">{error}</p>}

            <div className="text-center p-t-150">
              <span
                className="txt1"
                style={{ cursor: 'pointer' }}
                onClick={handleForgotPassword}
              >
                Forgot your password?
              </span>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};

export default Login;
