import axios from 'axios';
import rateLimit from 'axios-rate-limit';

// Create an axios instance with rate limiting
const api = rateLimit(
  axios.create({
    baseURL: import.meta.env.VITE_API_BASE,
  }),
  { 
    maxRequests: 3, // Maximum of 3 requests
    perMilliseconds: 60000, // Per 1 minute
  }
);

export default api;