import React, { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';

export default function Certificates() {
  const navigate = useNavigate();

  useEffect(() => {
    // Redirect to the Courses page with the certificates tab selected (path-based)
    navigate('/courses/certificates', { replace: true });
  }, [navigate]);

  return null;
}