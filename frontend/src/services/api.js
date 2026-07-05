import axios from "axios";

const API_BASE_URL = "http://localhost:5000/api";

const api = axios.create({
  baseURL: API_BASE_URL,
});

export const getEvents = async (filters = {}) => {
  const { severity, sourceIp, search, page = 1, pageSize = 20 } = filters;
  const params = { page, pageSize };
  if (severity) params.severity = severity;
  if (sourceIp) params.sourceIp = sourceIp;
  if (search) params.search = search;

  const response = await api.get("/events", { params });
  return response.data;
};

export const getEventsByIp = async (ip, page = 1, pageSize = 20) => {
  const response = await api.get("/events", {
    params: { sourceIp: ip, page, pageSize },
  });
  return response.data;
};

export const getStats = async () => {
  const response = await api.get("/events/stats");
  return response.data;
};

export default api;

export const getStatistics = async () => {
  const response = await api.get("/events/statistics");
  return response.data;
};
