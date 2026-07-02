import axios from 'axios';

const API_BASE_URL = 'http://localhost:5000/api';

const api = axios.create({
  baseURL: API_BASE_URL,
});

export const getEvents = async (filters = {}) => {
  const { severity, sourceIp, limit } = filters;
  const params = {};
  if (severity) params.severity = severity;
  if (sourceIp) params.sourceIp = sourceIp;
  if (limit) params.limit = limit;

  const response = await api.get('/events', { params });
  return response.data;
};

export const getEventsByIp = async (ip) => {
  const response = await api.get('/events', {
    params: { sourceIp: ip, limit: 200 }
  });
  return response.data;
};

export const getStats = async () => {
  const response = await api.get('/events/stats');
  return response.data;
};

export default api;
