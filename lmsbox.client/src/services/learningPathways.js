// Learning Pathways service: manage pathways, course mappings, and user assignments
// Expected backend endpoints:
// GET /api/AdminLearningPathways -> { items: [{ id, name, description, courseCount, memberCount }] }
// GET /api/AdminLearningPathways/:id -> { id, name, description, courses: [], members: [] }
// POST /api/AdminLearningPathways -> { id }
// PUT /api/AdminLearningPathways/:id -> success
// DELETE /api/AdminLearningPathways/:id -> success

import api from '../utils/api';

export async function listUserGroups(params = {}) {
  const { 
    page = 1, 
    pageSize = 10, 
    search = '', 
    sortBy = 'name', 
    sortOrder = 'asc' 
  } = params;
  
  const queryParams = new URLSearchParams();
  queryParams.append('page', page);
  queryParams.append('pageSize', pageSize);
  if (search?.trim()) {
    queryParams.append('search', search.trim());
  }
  if (sortBy) queryParams.append('sortBy', sortBy);
  if (sortOrder) queryParams.append('sortOrder', sortOrder);
  
  try {
    const response = await api.get(`/api/AdminLearningPathways?${queryParams.toString()}`);
    const data = response.data;
    const items = Array.isArray(data?.items) ? data.items : Array.isArray(data) ? data : [];
    
    // Transform API response to match UI expectations
    return {
      items: items.map(item => ({
        id: item.id,
        name: item.name,
        description: item.description,
        courseCount: item.courseCount || 0,
        userCount: item.memberCount || 0,
        createdAt: item.createdAt,
        createdBy: item.createdBy
      })),
      pagination: data.pagination || {
        currentPage: page,
        pageSize,
        totalPages: 1,
        totalCount: items.length,
        hasNextPage: false,
        hasPreviousPage: false
      }
    };
  } catch (err) {
    console.error('Error fetching user groups:', err);
    // Mock fallback
    const mock = [
      { id: 'ug-1', name: 'Marketing Team', description: 'All marketing department staff', courseCount: 3, userCount: 12 },
      { id: 'ug-2', name: 'Sales Team', description: 'Sales representatives', courseCount: 2, userCount: 8 },
      { id: 'ug-3', name: 'Engineering', description: 'Development and QA', courseCount: 5, userCount: 25 },
      { id: 'ug-4', name: 'New Hires 2025', description: 'Onboarding group', courseCount: 4, userCount: 15 },
    ];
    const searchTerm = search?.trim();
    const filtered = !searchTerm ? mock : mock.filter((m) => 
      m.name.toLowerCase().includes(searchTerm.toLowerCase()) || 
      (m.description || '').toLowerCase().includes(searchTerm.toLowerCase())
    );
    
    return {
      items: filtered,
      pagination: {
        currentPage: 1,
        pageSize: filtered.length,
        totalPages: 1,
        totalCount: filtered.length,
        hasNextPage: false,
        hasPreviousPage: false
      }
    };
  }
}

export async function getUserGroup(groupId) {
  try {
    const response = await api.get(`/api/AdminLearningPathways/${encodeURIComponent(groupId)}`);
    const data = response.data;
    
    console.log('🔍 getUserGroup raw response:', data);
    
    // Return full data including arrays for UI display
    return {
      id: data.id,
      name: data.name,
      description: data.description,
      courses: data.courses || [],
      members: data.members || [],
      courseIds: (data.courses || []).map(c => c.id),
      userIds: (data.members || []).map(m => m.userId)
    };
  } catch (err) {
    console.error('❌ getUserGroup error:', err);
    // Mock
    return {
      id: groupId,
      name: 'Sample Group',
      description: 'Mock group for editing',
      courses: [
        { id: 'c1', title: 'Cyber Security Essentials', description: 'Learn security basics' },
        { id: 'c2', title: 'GDPR Compliance', description: 'Data protection' }
      ],
      members: [
        { userId: 'u1', userName: 'Alice Johnson', email: 'alice@example.com' },
        { userId: 'u2', userName: 'Bob Smith', email: 'bob@example.com' }
      ],
      courseIds: ['c1', 'c2'],
      userIds: ['u1', 'u2']
    };
  }
}

export async function saveUserGroup(group, isEdit = false) {
  try {
    const payload = {
      name: group.name,
      description: group.description,
      courseIds: group.courseIds || [],
      userIds: group.userIds || []
    };

    let response;
    if (isEdit) {
      response = await api.put(`/api/AdminLearningPathways/${group.id}`, payload);
      return { success: true };
    } else {
      response = await api.post('/api/AdminLearningPathways', payload);
      return response.data;
    }
  } catch (_err) {
    // Mock fallback
    await new Promise((r) => setTimeout(r, 500));
    return { id: group.id || 'ug-' + Math.random().toString(36).slice(2, 8) };
  }
}

export async function deleteUserGroup(groupId) {
  try {
    await api.delete(`/api/AdminLearningPathways/${encodeURIComponent(groupId)}`);
    return { success: true };
  } catch (_err) {
    // Mock fallback
    await new Promise((r) => setTimeout(r, 300));
    return { success: true };
  }
}

// For selecting users to add to group (returns user list)
export async function listUsers(search = '', page = 1, pageSize = 50) {
  const params = new URLSearchParams();
  params.append('page', page);
  params.append('pageSize', pageSize);
  if (search?.trim()) {
    params.append('search', search.trim());
  }
  
  try {
    const response = await api.get(`/api/admin/users?${params.toString()}`);
    const data = response.data;
    return Array.isArray(data?.items) ? data.items : Array.isArray(data) ? data : [];
  } catch {
    // Mock
    const mock = [
      { id: 'u1', firstName: 'Alice', lastName: 'Johnson', email: 'alice@example.com' },
      { id: 'u2', firstName: 'Bob', lastName: 'Smith', email: 'bob@example.com' },
      { id: 'u3', firstName: 'Carol', lastName: 'Davis', email: 'carol@example.com' },
    ];
    const searchTerm = search?.trim();
    if (!searchTerm) return mock;
    const lower = searchTerm.toLowerCase();
    return mock.filter((u) => 
      (u.firstName && u.firstName.toLowerCase().includes(lower)) ||
      (u.lastName && u.lastName.toLowerCase().includes(lower)) ||
      u.email.toLowerCase().includes(lower)
    );
  }
}

// For selecting courses to map to group (only show published courses)
export async function listCoursesForMapping(search = '') {
  const params = new URLSearchParams();
  if (search?.trim()) {
    params.append('search', search.trim());
  }
  // Only show published courses for pathway mapping
  params.append('status', 'published');
  params.append('pageSize', '100'); // Get more courses for selection
  
  console.log('🔍 Fetching courses with params:', params.toString());
  
  try {
    const response = await api.get(`/api/admin/courses?${params.toString()}`);
    const data = response.data;
    console.log('📦 API Response:', data);
    // Handle both response formats: {items: [...]} and {courses: [...]}
    const courses = Array.isArray(data?.items) ? data.items : 
                    Array.isArray(data?.courses) ? data.courses :
                    Array.isArray(data) ? data : [];
    console.log('✅ Parsed courses:', courses);
    return courses;
  } catch (error) {
    console.error('❌ Error fetching courses:', error);
    return [];
    const mock = [
      { id: 'c1', title: 'Cyber Security Essentials', category: 'Security' },
      { id: 'c2', title: 'GDPR Compliance', category: 'Compliance' },
      { id: 'c3', title: 'Effective Communication', category: 'Soft Skills' },
      { id: 'c4', title: 'Employee Engagement', category: 'HR' },
    ];
    const searchTerm = search?.trim();
    if (!searchTerm) return mock;
    const lower = searchTerm.toLowerCase();
    return mock.filter((c) => c.title.toLowerCase().includes(lower));
  }
}
