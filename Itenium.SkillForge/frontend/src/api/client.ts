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

// ── Goals ──────────────────────────────────────────────────────────────────

export interface Skill {
  id: number;
  name: string;
  category: string;
  description: string | null;
  levelCount: number;
  levelDescriptorsJson: string;
  prerequisitesJson: string;
  createdAt: string;
  profiles: number[];
}

export interface SkillDetail {
  id: number;
  name: string;
  category: string;
  description: string | null;
  levelCount: number;
  levelDescriptors: string[];
  prerequisites: { skillId: number; requiredNiveau: number }[];
  prerequisiteWarnings: {
    skillId: number;
    skillName: string;
    requiredNiveau: number;
    warningText: string;
  }[];
  createdAt: string;
  profiles: number[];
}

export type GoalStatus = 'Active' | 'Completed' | 'Cancelled';

export interface Goal {
  id: string;
  consultantId: string;
  coachId: string;
  skillId: number;
  skill: Skill | null;
  currentNiveau: number;
  targetNiveau: number;
  deadline: string;
  createdAt: string;
  status: GoalStatus;
  linkedResourceIds: string | null;
}

export interface CreateGoalPayload {
  consultantId: string;
  coachId: string;
  skillId: number;
  currentNiveau: number;
  targetNiveau: number;
  deadline: string;
  linkedResourceIds: string | null;
}

export async function fetchGoals(consultantId?: string): Promise<Goal[]> {
  const params = consultantId ? { consultantId } : {};
  const response = await api.get<Goal[]>('/api/goals', { params });
  return response.data;
}

export async function fetchMyGoals(consultantId: string): Promise<Goal[]> {
  const response = await api.get<Goal[]>('/api/goals/mine', { params: { consultantId } });
  return response.data;
}

export async function createGoal(payload: CreateGoalPayload): Promise<Goal> {
  const response = await api.post<Goal>('/api/goals', payload);
  return response.data;
}

export async function raiseReadinessFlag(goalId: string, consultantId: string): Promise<void> {
  await api.post(`/api/goals/${goalId}/readiness-flag`, null, { params: { consultantId } });
}

export async function lowerReadinessFlag(goalId: string, consultantId: string): Promise<void> {
  await api.delete(`/api/goals/${goalId}/readiness-flag`, { params: { consultantId } });
}

// ── Coach Dashboard ──────────────────────────────────────────────────────────

export interface CoachDashboardItem {
  consultantId: string;
  activeGoalCount: number;
  readinessFlagAgeInDays: number | null;
  lastActivityAt: string;
  isInactive: boolean;
}

export async function fetchCoachDashboard(coachId: string): Promise<CoachDashboardItem[]> {
  const response = await api.get<CoachDashboardItem[]>('/api/dashboard/coach', { params: { coachId } });
  return response.data;
}

// ── Admin Users API ───────────────────────────────────────────────────────────

export interface UserRecord {
  id: string;
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  teamIds: number[];
  isArchived: boolean;
  archivedAt: string | null;
}

export interface CreateUserPayload {
  userName: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  role: string;
  teamIds: number[];
}

export async function fetchUsers(): Promise<UserRecord[]> {
  const response = await api.get<UserRecord[]>('/api/users');
  return response.data;
}

export async function createUser(payload: CreateUserPayload): Promise<UserRecord> {
  const response = await api.post<UserRecord>('/api/users', payload);
  return response.data;
}

export async function archiveUser(id: string): Promise<void> {
  await api.post(`/api/users/${id}/archive`);
}

export async function restoreUser(id: string): Promise<void> {
  await api.post(`/api/users/${id}/restore`);
}

export async function fetchOrphanedConsultants(): Promise<UserRecord[]> {
  const response = await api.get<UserRecord[]>('/api/users/orphaned-consultants');
  return response.data;
}

// ── Activity ─────────────────────────────────────────────────────────────────

export type ActivityType = 'GoalCreated' | 'ReadinessFlagRaised' | 'ResourceCompleted' | 'ValidationReceived';

export interface ActivityFeedItem {
  consultantId: string;
  type: ActivityType;
  occurredAt: string;
  description: string;
  referenceId: string | null;
}

export async function fetchConsultantActivity(consultantId: string): Promise<ActivityFeedItem[]> {
  const response = await api.get<ActivityFeedItem[]>(`/api/consultants/${consultantId}/activity`);
  return response.data;
}

// ── Resource Library (Stories #25-#28) ──────────────────────────────────────

export type ResourceType = 'Article' | 'Video' | 'Book' | 'Course' | 'Other';

export const ResourceTypeValues: Record<ResourceType, number> = {
  Article: 1,
  Video: 2,
  Book: 3,
  Course: 4,
  Other: 5,
};

export interface Resource {
  id: string;
  title: string;
  url: string;
  type: string;
  skillId: number;
  fromNiveau: number;
  toNiveau: number;
  contributedBy: string;
  contributedAt: string;
  thumbsUp: number;
  thumbsDown: number;
}

export interface CreateResourcePayload {
  title: string;
  url: string;
  type: number;
  skillId: number;
  fromNiveau: number;
  toNiveau: number;
}

export interface ResourceCompletion {
  id: string;
  resourceId: string;
  consultantId: string;
  goalId: string;
  completedAt: string;
}

export async function fetchResources(filters?: {
  skillId?: number;
  fromNiveau?: number;
  toNiveau?: number;
}): Promise<Resource[]> {
  const params: Record<string, string> = {};
  if (filters?.skillId !== undefined) params['skillId'] = String(filters.skillId);
  if (filters?.fromNiveau !== undefined) params['fromNiveau'] = String(filters.fromNiveau);
  if (filters?.toNiveau !== undefined) params['toNiveau'] = String(filters.toNiveau);
  const response = await api.get<Resource[]>('/api/resources', { params });
  return response.data;
}

export async function createResource(data: CreateResourcePayload): Promise<Resource> {
  const response = await api.post<Resource>('/api/resources', data);
  return response.data;
}

export async function completeResource(resourceId: string, goalId: string): Promise<ResourceCompletion> {
  const response = await api.post<ResourceCompletion>(`/api/resources/${resourceId}/complete`, { goalId });
  return response.data;
}

export async function rateResource(
  resourceId: string,
  rating: 'up' | 'down',
): Promise<{ thumbsUp: number; thumbsDown: number }> {
  const response = await api.post<{ thumbsUp: number; thumbsDown: number }>(`/api/resources/${resourceId}/rate`, {
    rating,
  });
  return response.data;
}

// ── Live Sessions (Stories #31-#33) ──────────────────────────────────────────

export interface Session {
  id: string;
  coachId: string;
  consultantId: string;
  startedAt: string;
  endedAt: string | null;
  notes: string | null;
}

export interface SessionFocus {
  sessionId: string;
  consultantId: string;
  startedAt: string;
  activeGoals: Goal[];
  pendingReadinessFlags: ReadinessFlag[];
}

export interface ReadinessFlag {
  id: string;
  goalId: string;
  consultantId: string;
  raisedAt: string;
}

export interface Validation {
  id: string;
  skillId: number;
  consultantId: string;
  validatedBy: string;
  validatedAt: string;
  fromNiveau: number;
  toNiveau: number;
  sessionId: string | null;
  notes: string | null;
}

export interface CreateValidationPayload {
  skillId: number;
  consultantId: string;
  fromNiveau: number;
  toNiveau: number;
  sessionId: string | null;
  notes: string | null;
}

export async function startSession(consultantId: string): Promise<Session> {
  const response = await api.post<Session>('/api/sessions', { consultantId });
  return response.data;
}

export async function getSessionFocus(sessionId: string): Promise<SessionFocus> {
  const response = await api.get<SessionFocus>(`/api/sessions/${sessionId}/focus`);
  return response.data;
}

export async function endSession(sessionId: string): Promise<Session> {
  const response = await api.put<Session>(`/api/sessions/${sessionId}/end`);
  return response.data;
}

export async function updateSessionNotes(sessionId: string, notes: string): Promise<Session> {
  const response = await api.put<Session>(`/api/sessions/${sessionId}/notes`, { notes });
  return response.data;
}

export async function createValidation(data: CreateValidationPayload): Promise<Validation> {
  const response = await api.post<Validation>('/api/validations', data);
  return response.data;
}

export async function fetchSkills(profile?: number): Promise<Skill[]> {
  const params = profile !== undefined ? { profile } : {};
  const response = await api.get<Skill[]>('/api/skills', { params });
  return response.data;
}

export async function fetchSkill(id: number): Promise<SkillDetail> {
  const response = await api.get<SkillDetail>(`/api/skills/${id}`);
  return response.data;
}

// ── Roadmap (Story #20) ───────────────────────────────────────────────────────

export type RoadmapNodeStatus = 'Active' | 'Locked' | 'Complete';

export interface PrerequisiteWarning {
  skillId: number;
  skillName: string;
  requiredNiveau: number;
  warningText: string;
}

export interface RoadmapNode {
  skillId: number;
  skillName: string;
  category: string;
  levelCount: number;
  currentNiveau: number;
  targetNiveau: number | null;
  status: RoadmapNodeStatus;
  prerequisiteWarnings: PrerequisiteWarning[];
}

export interface RoadmapResponse {
  userId: string;
  profile: number | null;
  nodes: RoadmapNode[];
  totalSkillCount: number;
  showAll: boolean;
}

export async function fetchRoadmap(userId: string, showAll = false): Promise<RoadmapResponse> {
  const response = await api.get<RoadmapResponse>('/api/roadmap', {
    params: { userId, showAll },
  });
  return response.data;
}

// ── Seniority (Stories #34, #35) ─────────────────────────────────────────────

export interface SeniorityThresholdDto {
  id: number;
  seniorityLevel: number;
  skillId: number;
  skillName: string;
  minNiveau: number;
}

export interface SeniorityRulesetResponse {
  profile: number;
  thresholds: SeniorityThresholdDto[];
}

export interface UnmetRequirement {
  skillId: number;
  skillName: string;
  minNiveau: number;
  currentNiveau: number;
}

export interface SeniorityProgressResponse {
  userId: string;
  profile: number | null;
  currentLevel: number | null;
  targetLevel: number | null;
  metCount: number;
  requiredCount: number;
  unmetRequirements: UnmetRequirement[];
}

export async function fetchSeniorityProgress(userId: string): Promise<SeniorityProgressResponse> {
  const response = await api.get<SeniorityProgressResponse>('/api/seniority/progress', {
    params: { userId },
  });
  return response.data;
}

// ── Consultants / Profile assignment (Story #19) ──────────────────────────────

export interface ConsultantProfileResponse {
  userId: string;
  profile: number;
  assignedAt: string;
  assignedBy: string | null;
}

export async function fetchConsultantProfile(userId: string): Promise<ConsultantProfileResponse | null> {
  try {
    const response = await api.get<ConsultantProfileResponse>(`/api/consultants/${userId}/profile`);
    return response.data;
  } catch {
    return null;
  }
}

export async function assignConsultantProfile(
  userId: string,
  profile: number,
  assignedBy?: string,
): Promise<void> {
  await api.put(`/api/consultants/${userId}/profile`, { profile, assignedBy });
}
