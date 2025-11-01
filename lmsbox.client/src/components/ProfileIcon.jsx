import React from 'react';
import { useTheme } from '../theme/ThemeContext';

export default function ProfileIcon({ name = 'User' }) {
  const theme = useTheme();
  const tenant = theme?.tenant || {};
  
  // Get initials from name
  const getInitials = (name) => {
    if (!name) return 'U';
    const words = name.trim().split(' ');
    if (words.length === 1) {
      return words[0].charAt(0).toUpperCase();
    }
    return (words[0].charAt(0) + words[words.length - 1].charAt(0)).toUpperCase();
  };

  const initials = getInitials(name);

  return (
    <div 
      className="w-10 h-10 rounded-full flex items-center justify-center text-white font-semibold text-sm"
      style={{ backgroundColor: tenant?.primaryColor || '#3b82f6' }}
    >
      {initials}
    </div>
  );
}
