import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import config from '../../config';

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
      const response = await fetch(
        `${config.apiUrl}/ForgotPassword/forgot-password`,
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({ email }),
        },
      );

      const data = await response.json();

      if (!response.ok) {
        throw new Error(data.message || 'Erro ao enviar o e-mail.');
      }

      setMessage(
        data.message || 'Um link de redefinição foi enviado para seu e-mail.',
      );
    } catch (err: any) {
      setError(
        err.message || 'Erro ao processar sua solicitação. Tente novamente.',
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
            <span className="main-form-title">Esqueci minha senha</span>

            <form onSubmit={handleSubmit}>
              <div className="form-group">
                <div
                  className="wrap-input100 validate-input"
                  data-validate="Email válido: exemplo@dominio.com"
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
                    Verifique sua caixa de spam caso não encontre na caixa de
                    entrada.
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
                  {isLoading ? 'Enviando...' : 'Enviar Link'}
                </button>
              </div>

              <div className="text-center p-t-20">
                <span
                  className="txt1"
                  style={{ cursor: 'pointer' }}
                  onClick={() => navigate('/login')}
                >
                  Voltar ao Login
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
