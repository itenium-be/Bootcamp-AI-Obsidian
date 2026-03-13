import axios from 'axios';
import { useAuthStore } from '../stores';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

const api = axios.create({ baseURL: API_BASE_URL });

api.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) useAuthStore.getState().logout();
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
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
  });
  return response.data;
}

export interface Team {
  id: number;
  name: string;
}
export async function fetchUserTeams(): Promise<Team[]> {
  return (await api.get<Team[]>('/api/team')).data;
}

export interface Course {
  id: number;
  name: string;
  description: string | null;
  category: string | null;
  level: string | null;
}
export interface CourseFormData {
  name: string;
  description?: string;
  category?: string;
  level?: string;
}
export async function fetchCourses(): Promise<Course[]> {
  return (await api.get<Course[]>('/api/course')).data;
}
export async function createCourse(data: CourseFormData): Promise<Course> {
  return (await api.post<Course>('/api/course', data)).data;
}
export async function updateCourse(id: number, data: CourseFormData): Promise<Course> {
  return (await api.put<Course>(`/api/course/${id}`, data)).data;
}
export async function deleteCourse(id: number): Promise<void> {
  await api.delete(`/api/course/${id}`);
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
  return (await api.get<User[]>('/api/user')).data;
}

export interface Enrollment {
  id: number;
  learnerId: string;
  courseId: number;
  enrolledAt: string;
  completedAt: string | null;
}
export async function fetchEnrollments(): Promise<Enrollment[]> {
  return (await api.get<Enrollment[]>('/api/enrollment')).data;
}
export async function enrollInCourse(courseId: number): Promise<Enrollment> {
  return (await api.post<Enrollment>(`/api/enrollment/${courseId}`)).data;
}
export async function unenrollFromCourse(courseId: number): Promise<void> {
  await api.delete(`/api/enrollment/${courseId}`);
}

export interface Progress {
  id: number;
  learnerId: string;
  courseId: number;
  percentageComplete: number;
  lastUpdated: string;
  notes: string | null;
}
export async function fetchProgress(): Promise<Progress[]> {
  return (await api.get<Progress[]>('/api/progress')).data;
}
export async function updateProgress(
  courseId: number,
  data: { percentageComplete: number; notes?: string },
): Promise<Progress> {
  return (await api.put<Progress>(`/api/progress/${courseId}`, data)).data;
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
export async function fetchCertificates(): Promise<Certificate[]> {
  return (await api.get<Certificate[]>('/api/certificate')).data;
}

export interface Stats {
  totalCourses: number;
  totalLearners: number;
  totalEnrollments: number;
  totalCertificates: number;
  completionRate: number;
}
export async function fetchStats(): Promise<Stats> {
  return (await api.get<Stats>('/api/stats')).data;
}

// Team management
export interface TeamCreateData {
  name: string;
  description?: string;
}
export async function createTeam(data: TeamCreateData): Promise<Team> {
  return (await api.post<Team>('/api/team', data)).data;
}
export async function updateTeam(id: number, data: TeamCreateData): Promise<Team> {
  return (await api.put<Team>(`/api/team/${id}`, data)).data;
}
export async function deleteTeam(id: number): Promise<void> {
  await api.delete(`/api/team/${id}`);
}
export async function fetchTeamMembers(teamId: number): Promise<User[]> {
  return (await api.get<User[]>(`/api/team/${teamId}/members`)).data;
}
export async function addTeamMember(teamId: number, userId: string): Promise<void> {
  await api.post(`/api/team/${teamId}/members`, { userId });
}
export async function removeTeamMember(teamId: number, userId: string): Promise<void> {
  await api.delete(`/api/team/${teamId}/members/${userId}`);
}

// User management
export interface UserCreateData {
  userName: string;
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
  roles: string[];
}
export async function createUser(data: UserCreateData): Promise<User> {
  return (await api.post<User>('/api/user', data)).data;
}
export async function deleteUser(id: string): Promise<void> {
  await api.delete(`/api/user/${id}`);
}

// CourseTeam assignment
export interface CourseTeam {
  courseId: number;
  teamId: number;
  assignedAt: string;
}
export async function fetchCourseTeams(courseId: number): Promise<CourseTeam[]> {
  return (await api.get<CourseTeam[]>(`/api/courseteam/course/${courseId}`)).data;
}
export async function assignTeamToCourse(courseId: number, teamId: number): Promise<void> {
  await api.post('/api/courseteam', { courseId, teamId });
}
export async function removeTeamFromCourse(courseId: number, teamId: number): Promise<void> {
  await api.delete(`/api/courseteam/${courseId}/${teamId}`);
}

// Feedback/Ratings
export interface CourseFeedback {
  id: number;
  learnerId: string;
  courseId: number;
  courseName?: string;
  rating: number;
  comment: string | null;
  submittedAt: string;
}
export interface FeedbackSummary {
  courseId: number;
  courseName: string;
  averageRating: number;
  totalRatings: number;
}
export interface CourseFeedbackDetail {
  averageRating: number;
  totalRatings: number;
  feedback: CourseFeedback[];
}
export interface MonthlyStats {
  year: number;
  month: number;
  enrollments: number;
  completions: number;
}

export async function fetchFeedback(): Promise<CourseFeedback[]> {
  return (await api.get<CourseFeedback[]>('/api/feedback')).data;
}
export async function fetchCourseFeedback(courseId: number): Promise<CourseFeedbackDetail> {
  return (await api.get<CourseFeedbackDetail>(`/api/feedback/course/${courseId}`)).data;
}
export async function submitFeedback(data: {
  courseId: number;
  rating: number;
  comment?: string;
}): Promise<CourseFeedback> {
  return (await api.post<CourseFeedback>('/api/feedback', data)).data;
}
export async function fetchFeedbackSummary(): Promise<FeedbackSummary[]> {
  return (await api.get<FeedbackSummary[]>('/api/feedback/summary')).data;
}
export async function fetchMonthlyStats(): Promise<MonthlyStats[]> {
  return (await api.get<MonthlyStats[]>('/api/stats/monthly')).data;
}
export async function exportUsageReport(): Promise<Blob> {
  return (await api.get<Blob>('/api/stats/export/usage', { responseType: 'blob' })).data;
}
export async function exportCompletionReport(): Promise<Blob> {
  return (await api.get<Blob>('/api/stats/export/completion', { responseType: 'blob' })).data;
}
