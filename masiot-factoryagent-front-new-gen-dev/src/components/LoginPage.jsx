import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { userAPI } from '../api/userAPI';
import './LoginPage.css';

function LoginPage({ onLogin }) {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');

    // Клиентская валидация
    if (username.trim().length < 3) {
      setError('Логин должен содержать не менее 3 символов');
      return;
    }
    if (password.length < 6) {
      setError('Пароль должен содержать не менее 6 символов');
      return;
    }

    setLoading(true);

    try {
      const user = await userAPI.login(username, password);
      localStorage.setItem('currentUser', JSON.stringify(user));
      onLogin(user);
      navigate('/');
    } catch (err) {
      const msg = err.response?.data?.message;
      if (!err.response) {
        setError('Нет связи с API (http://localhost:3001). Запустите backend или проверьте CORS.');
      } else if (err.response?.status === 400 && err.response?.data?.errors) {
        const validationErrors = Object.values(err.response.data.errors).flat().join('; ');
        setError(validationErrors || 'Ошибка входа');
      } else {
        setError(msg || 'Ошибка входа');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-page">
      <div className="login-container">
        <div className="login-card">
          <h1 className="login-title">MAS-IOT</h1>
          <p className="login-subtitle">Вход в систему</p>
          <p className="login-hint">Локально: admin / admin123</p>

          <form onSubmit={handleSubmit} className="login-form">
            <div className="form-group">
              <label htmlFor="username">Логин</label>
              <input
                type="text"
                id="username"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                placeholder="Введите логин"
                required
                disabled={loading}
              />
            </div>

            <div className="form-group">
              <label htmlFor="password">Пароль</label>
              <input
                type="password"
                id="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Введите пароль"
                required
                disabled={loading}
              />
            </div>

            {error && <div className="error-message">{error}</div>}

            <button type="submit" className="login-button" disabled={loading}>
              {loading ? 'Вход...' : 'Войти'}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}

export default LoginPage;
