import { API_BASE } from '../utils/apiBase';

const getAuthHeaders = () => {
    const token = localStorage.getItem('token');
    return {
        'Content-Type': 'application/json',
        'Authorization': token ? `Bearer ${token}` : ''
    };
};

const getAuthHeadersForUpload = () => {
    const token = localStorage.getItem('token');
    return {
        'Authorization': token ? `Bearer ${token}` : ''
    };
};

export const getOrganisationSettings = async () => {
    const response = await fetch(`${API_BASE}/api/OrganisationSettings`, {
        method: 'GET',
        headers: getAuthHeaders()
    });

    if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message || 'Failed to fetch organisation settings');
    }

    return response.json();
};

export const updateOrganisationSettings = async (data) => {
    const response = await fetch(`${API_BASE}/api/OrganisationSettings`, {
        method: 'PUT',
        headers: getAuthHeaders(),
        body: JSON.stringify(data)
    });

    if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message || 'Failed to update organisation settings');
    }

    return response.json();
};

export const uploadBannerImage = async (imageFile) => {
    const formData = new FormData();
    formData.append('image', imageFile);

    const response = await fetch(`${API_BASE}/api/OrganisationSettings/upload-banner`, {
        method: 'POST',
        headers: getAuthHeadersForUpload(),
        body: formData
    });

    if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message || 'Failed to upload banner image');
    }

    return response.json();
};
