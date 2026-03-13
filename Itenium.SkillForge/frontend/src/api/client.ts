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

interface Course {
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

// ── Skill Catalogue ──────────────────────────────────────────────────────────

export interface SkillLevelDescriptor {
  id: number;
  level: number;
  description: string;
}

export interface SkillPrerequisite {
  id: number;
  skillId: number;
  prerequisiteSkillId: number;
  requiredLevel: number;
  prerequisiteSkill: Skill;
}

export interface Skill {
  id: number;
  name: string;
  description: string | null;
  category: string | null;
  levelCount: number;
  isUniversal: boolean;
  levelDescriptors: SkillLevelDescriptor[];
  prerequisites: SkillPrerequisite[];
}

export async function fetchSkills(): Promise<Skill[]> {
  const response = await api.get<Skill[]>('/api/skill');
  return response.data;
}

export interface SkillFormData {
  name: string;
  description: string | null;
  category: string | null;
  levelCount: number;
  isUniversal: boolean;
}

export async function createSkill(data: SkillFormData): Promise<Skill> {
  const response = await api.post<Skill>('/api/skill', { ...data, levelDescriptors: [] });
  return response.data;
}

export async function updateSkill(id: number, data: SkillFormData): Promise<Skill> {
  const response = await api.put<Skill>(`/api/skill/${id}`, data);
  return response.data;
}

export async function deleteSkill(id: number): Promise<void> {
  await api.delete(`/api/skill/${id}`);
}

// ── Prerequisite check ───────────────────────────────────────────────────────

export interface PrerequisiteCheckItem {
  skillName: string;
  requiredLevel: number;
  currentLevel: number;
}

// ── Roadmap ──────────────────────────────────────────────────────────────────

export interface RoadmapSkillItem {
  skillId: number;
  name: string;
  category: string | null;
  levelCount: number;
  achievedLevel: number;
  unmetPrerequisites: PrerequisiteCheckItem[];
}

interface RoadmapResponse {
  skills: RoadmapSkillItem[];
}

export async function fetchRoadmap(userId: string, showAll?: boolean): Promise<RoadmapResponse> {
  const response = await api.get<RoadmapResponse>(`/api/roadmap/${userId}`, {
    params: showAll !== undefined ? { showAll } : undefined,
  });
  return response.data;
}

export async function updateSkillProgress(userId: string, skillId: number, achievedLevel: number): Promise<void> {
  await api.put(`/api/roadmap/${userId}/progress`, { skillId, achievedLevel });
}

// ── Seniority ────────────────────────────────────────────────────────────────

interface SeniorityProgressItem {
  level: string;
  met: number;
  required: number;
}

export async function fetchSeniority(userId: string): Promise<SeniorityProgressItem[]> {
  const response = await api.get<SeniorityProgressItem[]>(`/api/seniority/${userId}`);
  return response.data;
}

// ── User management (admin) ──────────────────────────────────────────────────

interface CreateUserPayload {
  email: string;
  firstName: string;
  lastName: string;
  password: string;
  role: string;
  teamIds: number[];
}

export async function createUser(payload: CreateUserPayload): Promise<void> {
  await api.post('/api/user', payload);
}

interface UserSummary {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
}

export async function fetchUsers(): Promise<UserSummary[]> {
  const response = await api.get<UserSummary[]>('/api/user');
  return response.data;
}

export async function archiveUser(userId: string): Promise<void> {
  await api.patch(`/api/user/${userId}/archive`);
}

export async function restoreUser(userId: string): Promise<void> {
  await api.patch(`/api/user/${userId}/restore`);
}

export async function fetchArchivedUsers(): Promise<UserSummary[]> {
  const response = await api.get<UserSummary[]>('/api/user/archived');
  return response.data;
}
