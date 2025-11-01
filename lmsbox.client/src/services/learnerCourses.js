// Learner Courses service: fetch courses assigned to the current user
// Backend endpoints:
// GET /api/learner/courses?search=...&progress=... -> { items: [{ id, title, banner, progress, enrolledDate, lastAccessedDate, isCompleted, certificateEligible }], total }
// GET /api/learner/courses/certificates -> { items: [...], total }

export async function getMyCourses(search = '', progressFilter = 'all') {
  const params = new URLSearchParams();
  if (search?.trim()) params.append('search', search.trim());
  if (progressFilter && progressFilter !== 'all') params.append('progress', progressFilter);
  
  const url = `/api/learner/courses${params.toString() ? `?${params.toString()}` : ''}`;
  
  try {
    const res = await fetch(url, { 
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json'
      }
    });
    
    if (!res.ok) {
      throw new Error(`HTTP ${res.status}`);
    }
    
    const data = await res.json();
    return Array.isArray(data?.items) ? data.items : [];
  } catch (err) {
    console.warn('Failed to fetch courses from API, using mock data:', err.message);
    
    // Mock fallback data
    const mockCourses = [
      {
        id: 'c1',
        title: 'Cyber Security Essentials for UK Businesses (Level 1)',
        banner: '/src/assets/course-cover-1.png',
        progress: 99,
        enrolledDate: '2024-01-15T00:00:00Z',
        lastAccessedDate: '2024-10-30T14:30:00Z',
        isCompleted: false,
        certificateEligible: false
      },
      {
        id: 'c2',
        title: 'Effective Workplace Communication: Speak, Listen, Lead',
        banner: '/src/assets/course-cover-2.png',
        progress: 0,
        enrolledDate: '2024-09-10T00:00:00Z',
        lastAccessedDate: null,
        isCompleted: false,
        certificateEligible: false
      },
      {
        id: 'c3',
        title: 'Employee Engagement Through Transparent Communication',
        banner: '/src/assets/course-cover-3.png',
        progress: 1,
        enrolledDate: '2024-08-20T00:00:00Z',
        lastAccessedDate: '2024-08-25T10:15:00Z',
        isCompleted: false,
        certificateEligible: false
      },
      {
        id: 'c4',
        title: 'GDPR Compliance & Data Handling Best Practices',
        banner: '/src/assets/course-cover-4.png',
        progress: 10,
        enrolledDate: '2024-07-05T00:00:00Z',
        lastAccessedDate: '2024-10-28T16:45:00Z',
        isCompleted: false,
        certificateEligible: false
      }
    ];

    // Apply search filter
    let filtered = mockCourses;
    if (search?.trim()) {
      const q = search.toLowerCase();
      filtered = filtered.filter(c => c.title.toLowerCase().includes(q));
    }

    // Apply progress filter
    if (progressFilter === 'not_started') {
      filtered = filtered.filter(c => c.progress === 0);
    } else if (progressFilter === 'in_progress') {
      filtered = filtered.filter(c => c.progress > 0 && c.progress < 100);
    } else if (progressFilter === 'completed') {
      filtered = filtered.filter(c => c.progress >= 100);
    }

    return filtered;
  }
}

export async function getMyCertificates() {
  const url = '/api/learner/courses/certificates';
  
  try {
    const res = await fetch(url, { 
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json'
      }
    });
    
    if (!res.ok) {
      throw new Error(`HTTP ${res.status}`);
    }
    
    const data = await res.json();
    return Array.isArray(data?.items) ? data.items : [];
  } catch (err) {
    console.warn('Failed to fetch certificates from API, using mock data:', err.message);
    
    // Mock fallback - return completed courses
    return [
      {
        id: 'c1',
        title: 'Cyber Security Essentials for UK Businesses (Level 1)',
        banner: '/src/assets/course-cover-1.png',
        progress: 100,
        enrolledDate: '2024-01-15T00:00:00Z',
        lastAccessedDate: '2024-10-15T14:30:00Z',
        isCompleted: true,
        certificateEligible: true,
        certificateIssuedDate: '2024-10-15T14:30:00Z',
        certificateUrl: '/certificates/c1.pdf'
      }
    ];
  }
}
