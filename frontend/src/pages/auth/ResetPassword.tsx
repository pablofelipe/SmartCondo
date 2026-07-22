import React, { useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import config from '../../config';

import '../../styles/resetPassword.css';
import '../../styles/util.css';

const ResetPassword: React.FC = () => {
  const [searchParams] = useSearchParams();
  const [password, setPassword] = useState<string>('');
  const [confirmPassword, setConfirmPassword] = useState<string>('');
  const [message, setMessage] = useState<string>('');
  const [error, setError] = useState<string>('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const userId = searchParams.get('userId');
  const token = searchParams.get('token');

  useEffect(() => {
    if (!userId || !token) {
      navigate('/forgotPassword');
      setError('Invalid or expired link');
    }
  }, [userId, token, navigate]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (loading) return;

    if (password !== confirmPassword) {
      setError('Passwords do not match');
      return;
    }

    setLoading(true);
    try {
      const response = await fetch(
        `${config.apiUrl}/ForgotPassword/reset-password`,
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({
            userId,
            token,
            password: password,
          }),
        },
      );

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.message || 'Failed to reset the password.');
      }

      const data = await response.json();
      setMessage(data.message || 'Password reset successfully!');
      setError('');
    } catch (err: any) {
      setError(err.message || 'Failed to reset the password.');
      setMessage('');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="limiter">
      <div className="container-main">
        <div className="wrap-main">
          <div className="reset-password-container">
            <span className="main-form-title">Reset Password</span>
            <form onSubmit={handleSubmit}>
              <div className="form-group">
                <div className="wrap-input100 validate-input">
                  <input
                    className={`input100 ${password ? 'has-val' : ''}`}
                    type="password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    required
                    minLength={6}
                  />
                  <span
                    className="focus-input100"
                    data-placeholder="New Password"
                  ></span>
                </div>
              </div>

              <div className="form-group">
                <div className="wrap-input100 validate-input">
                  <input
                    className={`input100 ${confirmPassword ? 'has-val' : ''}`}
                    type="password"
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    required
                    minLength={6}
                  />
                  <span
                    className="focus-input100"
                    data-placeholder="Confirm Password"
                  ></span>
                </div>
              </div>

              {message && <p className="success-message">{message}</p>}
              {error && <p className="error-message">{error}</p>}

              <div
                className="wrap-btn100-form-btn"
                style={{ backgroundColor: '#ff4d4d' }}
              >
                <div className="btn100-form-bgbtn"></div>
                <button
                  type="submit"
                  className="btn100-form-btn"
                  disabled={loading}
                >
                  Reset Password
                </button>
              </div>

              <div className="text-center p-t-20">
                <span
                  className="txt1"
                  style={{ cursor: 'pointer' }}
                  onClick={() => navigate('/login')}
                >
                  Back to Login
                </span>
              </div>
            </form>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ResetPassword;
