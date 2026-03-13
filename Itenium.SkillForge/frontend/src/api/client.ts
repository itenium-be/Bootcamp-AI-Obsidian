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

export interface Enrollment {
  id: number;
  learnerId: string;
  courseId: number;
  enrolledAt: string;
  completedAt: string | null;
}

export interface Progress {
  id: number;
  learnerId: string;
  courseId: number;
  percentageComplete: number;
  lastUpdated: string;
  notes: string | null;
}

export interface Certificate {
  id: number;
  learnerId: string;
  learnerName: string;
  courseId: number;
  courseName: string;
  issuedAt: string;
  certificateNumber: string;
}

export async function fetchEnrollments(): Promise<Enrollment[]> {
  const response = await api.get<Enrollment[]>('/api/enrollment');
  return response.data;
}

export async function enrollInCourse(courseId: number): Promise<Enrollment> {
  const response = await api.post<Enrollment>(`/api/enrollment/${courseId}`);
  return response.data;
}

export async function unenrollFromCourse(courseId: number): Promise<void> {
  await api.delete(`/api/enrollment/${courseId}`);
}

export async function fetchProgress(): Promise<Progress[]> {
  const response = await api.get<Progress[]>('/api/progress');
  return response.data;
}

export async function fetchCourseProgress(courseId: number): Promise<Progress> {
  const response = await api.get<Progress>(`/api/progress/${courseId}`);
  return response.data;
}

export async function updateProgress(
  courseId: number,
  data: { percentageComplete: number; notes?: string },
): Promise<Progress> {
  const response = await api.put<Progress>(`/api/progress/${courseId}`, data);
  return response.data;
}

export async function fetchCertificates(): Promise<Certificate[]> {
  const response = await api.get<Certificate[]>('/api/certificate');
  return response.data;
}

export interface User {
  id: string;
  userName: string;
  email: string;
  firstName: string | null;
  lastName: string | null;
  roles: string[];
}

export async function fetchUsers(): Promise<User[]> {
  const response = await api.get<User[]>('/api/user');
  return response.data;
}

export async function fetchUser(id: string): Promise<User> {
  const response = await api.get<User>(`/api/user/${id}`);
  return response.data;
}

export interface CourseFormData {
  name: string;
  description?: string;
  category?: string;
  level?: string;
}

export async function createCourse(data: CourseFormData): Promise<Course> {
  const response = await api.post<Course>('/api/course', data);
  return response.data;
}

export async function updateCourse(id: number, data: CourseFormData): Promise<Course> {
  const response = await api.put<Course>(`/api/course/${id}`, data);
  return response.data;
}

export async function deleteCourse(id: number): Promise<void> {
  await api.delete(`/api/course/${id}`);
}
