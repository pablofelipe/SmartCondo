import { fetchApi } from './api';

export interface DashboardStats {
  totalUsers: number;
  totalVehicles: number;
  recentNotifications: number;
}

export const getDashboardStats = async (): Promise<DashboardStats> => {
  return fetchApi<DashboardStats>('/dashboard/stats');
};
