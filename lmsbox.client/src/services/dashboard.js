import api from '../utils/api';

export async function getDashboardStats() {
  const res = await api.get('/api/admin/dashboard/stats');
  return res.data;
}
