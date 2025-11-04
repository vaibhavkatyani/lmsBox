import api from '../utils/api';

// User & Engagement Reports
export async function getUserActivityReport(params = {}) {
  const { startDate, endDate, minDaysDormant = 30 } = params;
  const queryParams = new URLSearchParams();
  
  if (startDate) queryParams.append('startDate', startDate);
  if (endDate) queryParams.append('endDate', endDate);
  queryParams.append('minDaysDormant', minDaysDormant.toString());
  
  const res = await api.get(`/api/admin/reports/user-activity?${queryParams}`);
  return res.data;
}

export async function getUserProgressReport(params = {}) {
  const { startDate, endDate } = params;
  const queryParams = new URLSearchParams();
  
  if (startDate) queryParams.append('startDate', startDate);
  if (endDate) queryParams.append('endDate', endDate);
  
  const res = await api.get(`/api/admin/reports/user-progress?${queryParams}`);
  return res.data;
}

// Course Analytics Reports
export async function getCourseEnrollmentReport(params = {}) {
  const { startDate, endDate } = params;
  const queryParams = new URLSearchParams();
  
  if (startDate) queryParams.append('startDate', startDate);
  if (endDate) queryParams.append('endDate', endDate);
  
  const res = await api.get(`/api/admin/reports/course-enrollment?${queryParams}`);
  return res.data;
}

export async function getCourseCompletionReport(params = {}) {
  const { startDate, endDate } = params;
  const queryParams = new URLSearchParams();
  
  if (startDate) queryParams.append('startDate', startDate);
  if (endDate) queryParams.append('endDate', endDate);
  
  const res = await api.get(`/api/admin/reports/course-completion?${queryParams}`);
  return res.data;
}

export async function getLessonAnalyticsReport(courseId) {
  const queryParams = courseId ? `?courseId=${courseId}` : '';
  const res = await api.get(`/api/admin/reports/lesson-analytics${queryParams}`);
  return res.data;
}

// Learning Pathway Reports
export async function getPathwayProgressReport(params = {}) {
  const { startDate, endDate } = params;
  const queryParams = new URLSearchParams();
  
  if (startDate) queryParams.append('startDate', startDate);
  if (endDate) queryParams.append('endDate', endDate);
  
  const res = await api.get(`/api/admin/reports/pathway-progress?${queryParams}`);
  return res.data;
}

export async function getPathwayAssignmentsReport() {
  const res = await api.get('/api/admin/reports/pathway-assignments');
  return res.data;
}

// Administrative Reports
export async function getContentUsageReport() {
  const res = await api.get('/api/admin/reports/content-usage');
  return res.data;
}

// Export utilities
export function exportToCSV(data, filename) {
  if (!data || data.length === 0) return;
  
  const headers = Object.keys(data[0]);
  const csvContent = [
    headers.join(','),
    ...data.map(row => headers.map(header => {
      const value = row[header];
      return typeof value === 'string' && value.includes(',') 
        ? `"${value}"` 
        : value;
    }).join(','))
  ].join('\n');
  
  const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
  const link = document.createElement('a');
  link.href = URL.createObjectURL(blob);
  link.download = `${filename}_${new Date().toISOString().split('T')[0]}.csv`;
  link.click();
}

export function exportToJSON(data, filename) {
  const jsonContent = JSON.stringify(data, null, 2);
  const blob = new Blob([jsonContent], { type: 'application/json' });
  const link = document.createElement('a');
  link.href = URL.createObjectURL(blob);
  link.download = `${filename}_${new Date().toISOString().split('T')[0]}.json`;
  link.click();
}
