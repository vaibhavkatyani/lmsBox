import ReCAPTCHA from 'react-google-recaptcha';
import { createRef } from 'react';

const recaptchaRef = createRef();

export const executeRecaptcha = async () => {
  try {
    const token = await recaptchaRef.current.executeAsync();
    return token;
  } catch (error) {
    console.error('reCAPTCHA execution failed:', error);
    return null;
  }
};

export const RecaptchaComponent = () => (
  <ReCAPTCHA
    ref={recaptchaRef}
    size="invisible"
    sitekey={import.meta.env.VITE_RECAPTCHA_SITE_KEY}
  />
);

export { recaptchaRef };