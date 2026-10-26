import axios from 'axios';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:3001/api';
const USERS_URL = `${API_URL}/users`;

// Добавляем auth-заголовки ко всем запросам axios
axios.interceptors.request.use((config) => {
  const storedUser = localStorage.getItem('currentUser');
  if (storedUser) {
    try {
      const user = JSON.parse(storedUser);
      if (user.id) config.headers['X-User-Id'] = String(user.id);
      if (user.role) config.headers['X-User-Role'] = user.role;
    } catch {
      // ignore
    }
  }
  return config;
});

export const userAPI = {
  login: async (username, password) => {
    const response = await axios.post(`${USERS_URL}/login`, { username, password });
    return response.data;
  },

  getAll: async () => {
    const response = await axios.get(USERS_URL);
    return response.data;
  },

  getById: async (id) => {
    const response = await axios.get(`${USERS_URL}/${id}`);
    return response.data;
  },

  create: async (userData) => {
    const response = await axios.post(USERS_URL, userData);
    return response.data;
  },

  update: async (id, userData) => {
    const response = await axios.put(`${USERS_URL}/${id}`, userData);
    return response.data;
  },

  delete: async (id) => {
    const response = await axios.delete(`${USERS_URL}/${id}`);
    return response.data;
  }
};
