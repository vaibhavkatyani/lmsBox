import { useForm } from 'react-hook-form';
import { useState, useEffect } from 'react';
import { useNavigate, Navigate } from 'react-router-dom';
import { setAuthToken, getLastVisitedPage, clearLastVisitedPage } from '../utils/auth';
import { useAuth } from '../hooks/useAuth';
import { useTheme } from '../theme/ThemeContext';
import lmsLogo from '../assets/lmsbox-logo.png'; 
import loginIllustration from '../assets/login-image.png';
import api from '../utils/api';
import { RecaptchaComponent, executeRecaptcha } from '../utils/recaptcha';

export default function Login() {
  const { register, handleSubmit, formState: { errors } } = useForm();
  const [status, setStatus] = useState('idle'); // idle, loading, success, error
  const [message, setMessage] = useState('');
  const { isAuthenticated } = useAuth();
  const theme = useTheme();
  const logoSrc = theme?.logo || lmsLogo;
  const tenantName = theme?.name || import.meta.env.VITE_APP_TITLE || 'LMS Box';
  const pageTitle = `${tenantName} - Login`;

  // Redirect if already authenticated
  if (isAuthenticated) {
    return <Navigate to="/dashboard" replace />;
  }

  useEffect(() => {
    document.title = pageTitle;
  }, [pageTitle]);

  const onSubmit = async (data) => {
    try {
      setStatus('loading');
      setMessage('');

      // Execute invisible reCAPTCHA
      const recaptchaToken = await executeRecaptcha();
      if (!recaptchaToken) {
        throw new Error('reCAPTCHA verification failed');
      }

      // Send request with recaptcha token
      await api.post('/auth/login', {
        email: data.email,
        recaptchaToken
      });

      setStatus('success');
      setMessage('Login link sent! Please check your email to continue.');
    } catch (error) {
      setStatus('error');
      if (error.message === 'reCAPTCHA verification failed') {
        setMessage('Security check failed. Please try again.');
      } else {
        setMessage(error.response?.data?.message || 'Failed to send Login link. Please try again.');
      }
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-login-page-bg px-4">
      <div className="grid lg:grid-cols-2 gap-8 max-w-6xl w-full items-center">
        
        {/* Left: Login Form */}
        <div className="bg-login-box-bg p-8 rounded-lg shadow-lg max-w-md w-full mx-auto">
          <div className="mb-8 text-center">
            <img src={logoSrc} alt={`${tenantName} Logo`} className="h-12 mx-auto mb-4" />
            <h1 className="text-3xl font-semibold text-login-box-text">Sign in</h1>
            <p className="text-login-box-text text-sm mt-2">
              Enter your email address to receive a Login link for instant access.
            </p>
          </div>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
            <div>
              <label className="block text-sm font-medium text-login-box-text mb-2">Email address</label>
              <input
                {...register('email', { 
                  required: 'Email is required',
                  pattern: {
                    value: /^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}$/i,
                    message: "Invalid email address"
                  }
                })}
                type="email"
                placeholder="Enter your email"
                className="w-full border border-login-input-border rounded-lg px-4 py-3 text-sm text-login-box-text focus:ring-2 focus:ring-(--tenant-primary)"
                disabled={status === 'loading' || status === 'success'}
              />
              {errors.email && <p className="text-red-500 text-sm mt-1">{errors.email.message}</p>}
            </div>

            {message && (
              <p className={`text-sm ${status === 'success' ? 'text-green-600' : 'text-red-500'}`}>
                {message}
              </p>
            )}

            <button 
              type="submit" 
              className={`w-full py-2.5 rounded-lg font-medium transition-colors text-login-btn-text ${
                status === 'loading'
                  ? 'bg-login-btn-bg/60 cursor-not-allowed'
                  : status === 'success'
                  ? 'bg-login-btn-bg cursor-not-allowed'
                  : 'bg-login-btn-bg hover:brightness-90'
              }`}
              disabled={status === 'loading' || status === 'success'}
            >
              {status === 'loading' ? 'Sending Login link...' : 
               status === 'success' ? 'Check your email' : 
               'Send Login link'}
            </button>

            {/* <p className="text-sm text-center text-login-box-text mt-6">
              Don't have an account?
              <a href="#" className="text-login-box-link-text font-medium hover:underline ml-1">Register here</a>
            </p> */}
          </form>
          <RecaptchaComponent />
        </div>

        {/* Right: Illustration */}
        <div className="hidden lg:block">
          <img
            src={loginIllustration}
            alt="Login Illustration"
            className="w-full max-w-lg mx-auto object-cover"
          />
        </div>
      </div>
    </div>
  );
}