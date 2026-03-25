const rawBaseUrl = import.meta.env.VITE_API_BASE_URL || 'http://clicknextbankapi.somee.com'
export const API_BASE_URL = rawBaseUrl.replace(/\/$/, '')