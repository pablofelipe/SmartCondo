import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { forgotPassword } from '../../services/authService';

import '../../styles/forgotPassword.css';
import '../../styles/util.css';

const ForgotPassword: React.FC = () => {
  const [email, setEmail] = useState<string>('');
  const [message, setMessage] = useState<string>('');
  const [error, setError] = useState<string>('');
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError('');
    setMessage('');

    try {
      const data = await forgotPassword(email);

      setMessage(
        data.message || 'A reset link has been sent to your e-mail.',
      );
    } catch (err: any) {
      setError(
        err.message || 'Failed to process your request. Please try again.',
      );
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="limiter">
      <div className="container-main">
        <div className="wrap-main">
          <div className="forgot-password-container">
            <span className="main-form-title">Forgot my password</span>

            <form onSubmit={handleSubmit}>
              <div className="form-group">
                <div
                  className="wrap-input100 validate-input"
                  data-validate="Valid e-mail: example@domain.com"
                >
                  <input
                    className={`input100 ${email ? 'has-val' : ''}`}
                    name="email"
                    type="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    required
                    autoFocus
                  />
                  <span
                    className="focus-input100"
                    data-placeholder="Email"
                  ></span>
                </div>
              </div>

              {message && (
                <div className="alert alert-success">
                  <p className="success-message">{message}</p>
                  <p>
                    Check your spam folder if you don't find it in your
                    inbox.
                  </p>
                </div>
              )}

              {error && <p className="error-message">{error}</p>}

              <div
                className="wrap-btn100-form-btn"
                style={{ backgroundColor: '#ff4d4d' }}
              >
                <div className="btn100-form-bgbtn"></div>
                <button
                  type="submit"
                  className="btn100-form-btn"
                  disabled={isLoading}
                >
                  {isLoading ? 'Sending...' : 'Send Link'}
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

export default ForgotPassword;
