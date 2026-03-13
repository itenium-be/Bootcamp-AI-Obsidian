import axios from 'axios';
import { useAuthStore } from '../stores';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

const api = axios.create({
  baseURL: API_BASE_URL,
});

// Add auth token to requests
api.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Handle 401 responses
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      useAuthStore.getState().logout();
    }
    return Promise.reject(error);
  },
);

interface LoginResponse {
  access_token: string;
  token_type: string;
  expires_in: number;
}

export async function loginApi(username: string, password: string): Promise<LoginResponse> {
  const params = new URLSearchParams();
  params.append('grant_type', 'password');
  params.append('username', username);
  params.append('password', password);
  params.append('client_id', 'skillforge-spa');
  params.append('scope', 'openid profile email');

  const response = await axios.post<LoginResponse>(`${API_BASE_URL}/connect/token`, params, {
    headers: {
      'Content-Type': 'application/x-www-form-urlencoded',
    },
  });

  return response.data;
}

interface Team {
  id: number;
  name: string;
}

export async function fetchUserTeams(): Promise<Team[]> {
  const response = await api.get<Team[]>('/api/team');
  return response.data;
}

export interface Course {
  id: number;
  name: string;
  description: string | null;
  category: string | null;
  level: string | null;
}

export async function fetchCourses(): Promise<Course[]> {
  const response = await api.get<Course[]>('/api/course');
  return response.data;
}

export interface Stats {
  totalCourses: number;
  totalLearners: number;
  totalEnrollments: number;
  totalCertificates: number;
  completionRate: number;
}

export async function fetchStats(): Promise<Stats> {
  const response = await api.get<Stats>('/api/stats');
  return response.data;
}

export interface Enrollment {
  id: number;
  learnerId: string;
  courseId: number;
  enrolledAt: string;
  completedAt: string | null;
}

export async function fetchEnrollments(): Promise<Enrollment[]> {
  const response = await api.get<Enrollment[]>('/api/enrollment');
  return response.data;
}

export interface Progress {
  id: number;
  learnerId: string;
  courseId: number;
  percentageComplete: number;
  lastUpdated: string;
}

export async function fetchProgress(): Promise<Progress[]> {
  const response = await api.get<Progress[]>('/api/progress');
  return response.data;
}

export interface Certificate {
  id: number;
  learnerId: string;
  courseId: number;
  issuedAt: string;
}

export async function fetchCertificates(): Promise<Certificate[]> {
  const response = await api.get<Certificate[]>('/api/certificate');
  return response.data;
}
