import React, { useState, useEffect } from 'react';
import { userAPI } from '../api/userAPI';
import './UsersPage.css';

function UsersPage() {
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [editingUser, setEditingUser] = useState(null);
  const [formData, setFormData] = useState({
    username: '',
    password: '',
    role: 'user',
    fullName: ''
  });

  useEffect(() => {
    loadUsers();
  }, []);

  const loadUsers = async () => {
    try {
      const data = await userAPI.getAll();
      setUsers(data);
    } catch (error) {
      console.error('Ошибка загрузки пользователей:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = () => {
    setEditingUser(null);
    setFormData({ username: '', password: '', role: 'user', fullName: '' });
    setShowModal(true);
  };

  const handleEdit = (user) => {
    setEditingUser(user);
    setFormData({
      username: user.username,
      password: '',
      role: user.role,
      fullName: user.fullName || ''
    });
    setShowModal(true);
  };

  const handleDelete = async (userId) => {
    if (!window.confirm('Вы уверены, что хотите удалить этого пользователя?')) {
      return;
    }

    try {
      await userAPI.delete(userId);
      loadUsers();
    } catch (error) {
      alert('Ошибка удаления пользователя');
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    try {
      if (editingUser) {
        const updateData = { role: formData.role, fullName: formData.fullName };
        if (formData.password) {
          updateData.password = formData.password;
        }
        await userAPI.update(editingUser.id, updateData);
      } else {
        await userAPI.create(formData);
      }
      setShowModal(false);
      loadUsers();
    } catch (error) {
      alert(error.response?.data?.message || 'Ошибка сохранения');
    }
  };

  if (loading) {
    return <div className="users-page">Загрузка...</div>;
  }

  return (
    <div className="users-page">
      <div className="users-header">
        <h1>Управление пользователями</h1>
        <button className="btn-create" onClick={handleCreate}>
          + Создать пользователя
        </button>
      </div>

      <div className="users-table-container">
        <table className="users-table">
          <thead>
            <tr>
              <th>ID</th>
              <th>Логин</th>
              <th>Пароль</th>
              <th>Полное имя</th>
              <th>Роль</th>
              <th>Дата создания</th>
              <th>Действия</th>
            </tr>
          </thead>
          <tbody>
            {users.map((user) => (
              <tr key={user.id}>
                <td>{user.id}</td>
                <td>{user.username}</td>
                <td>
                  <span style={{ color: '#999', fontStyle: 'italic' }}>
                    {user.password || '••••••••'}
                  </span>
                </td>
                <td>{user.fullName || '—'}</td>
                <td>
                  <span className={`role-badge role-${user.role}`}>
                    {user.role === 'admin' ? 'Администратор' : 'Пользователь'}
                  </span>
                </td>
                <td>{new Date(user.createdAt).toLocaleDateString('ru-RU')}</td>
                <td>
                  <div className="actions">
                    <button className="btn-edit" onClick={() => handleEdit(user)}>
                      Изменить
                    </button>
                    <button className="btn-delete" onClick={() => handleDelete(user.id)}>
                      Удалить
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {showModal && (
        <div className="modal-overlay" onClick={() => setShowModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <h2>{editingUser ? 'Редактировать пользователя' : 'Создать пользователя'}</h2>
            <form onSubmit={handleSubmit}>
              <div className="form-group">
                <label>Логин</label>
                <input
                  type="text"
                  value={formData.username}
                  onChange={(e) => setFormData({ ...formData, username: e.target.value })}
                  required
                  disabled={editingUser !== null}
                />
              </div>

              <div className="form-group">
                <label>Пароль {editingUser && '(оставьте пустым, чтобы не менять)'}</label>
                <input
                  type="password"
                  value={formData.password}
                  onChange={(e) => setFormData({ ...formData, password: e.target.value })}
                  required={!editingUser}
                />
              </div>

              <div className="form-group">
                <label>Полное имя</label>
                <input
                  type="text"
                  value={formData.fullName}
                  onChange={(e) => setFormData({ ...formData, fullName: e.target.value })}
                />
              </div>

              <div className="form-group">
                <label>Роль</label>
                <select
                  value={formData.role}
                  onChange={(e) => setFormData({ ...formData, role: e.target.value })}
                >
                  <option value="user">Пользователь</option>
                  <option value="admin">Администратор</option>
                </select>
              </div>

              <div className="modal-actions">
                <button type="button" className="btn-cancel" onClick={() => setShowModal(false)}>
                  Отмена
                </button>
                <button type="submit" className="btn-save">
                  {editingUser ? 'Сохранить' : 'Создать'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

export default UsersPage;
