import axios from 'axios';

const API_ORIGIN = new URL(import.meta.env.VITE_API_URL || 'http://localhost:5079').origin;

export function clearSession() {
  for (const key of ['token', 'role', 'fullName', 'userId']) localStorage.removeItem(key);
  for (const key of ['ai_conversation_id', 'ai_conversation_token', 'ai_conversation_actor']) sessionStorage.removeItem(key);
  window.dispatchEvent(new Event('auth-changed'));
}

export async function validatedSession() {
  const token = localStorage.getItem('token');
  if (!token) return { token: null, role: null };
  try {
    const { data } = await axios.get(API_ORIGIN + '/api/Auth/me', { timeout: 5000 });
    if (localStorage.getItem('token') !== token) return { token: null, role: null };
    const role = (data.roleCode || '').toLowerCase();
    localStorage.setItem('role', role);
    localStorage.setItem('userId', String(data.userId));
    localStorage.setItem('fullName', data.fullName || '');
    return { token, role };
  } catch (error) {
    if (error.response?.status === 401 && localStorage.getItem('token') === token) clearSession();
    return { token: null, role: null };
  }
}
