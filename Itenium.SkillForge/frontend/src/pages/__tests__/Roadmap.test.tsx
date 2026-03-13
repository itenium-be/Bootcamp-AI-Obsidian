import type { ReactElement } from 'react';
import { vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import * as client from '@/api/client';
import { Roadmap } from '../Roadmap';

// Minimal auth store mock
vi.mock('@/stores', () => ({
  useAuthStore: () => ({
    user: { id: 'user-lea', name: 'Lea', email: 'lea@test.local', isBackOffice: false },
    isAuthenticated: true,
  }),
}));

// i18n stub
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: { language: 'en' },
  }),
}));

function renderWithQuery(ui: ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>);
}

describe('Roadmap', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    vi.spyOn(client, 'fetchRoadmap').mockReturnValue(new Promise(() => {}));
    vi.spyOn(client, 'fetchSeniorityProgress').mockReturnValue(new Promise(() => {}));

    renderWithQuery(<Roadmap />);
    expect(screen.getByText('common.loading')).toBeTruthy();
  });

  it('shows empty state when no profile assigned', async () => {
    vi.spyOn(client, 'fetchRoadmap').mockResolvedValue({
      userId: 'user-lea',
      profile: null,
      nodes: [],
      totalSkillCount: 0,
      showAll: false,
    });
    vi.spyOn(client, 'fetchSeniorityProgress').mockResolvedValue({
      userId: 'user-lea',
      profile: null,
      currentLevel: null,
      targetLevel: null,
      metCount: 0,
      requiredCount: 0,
      unmetRequirements: [],
    });

    renderWithQuery(<Roadmap />);

    // Wait for the empty state message
    const noRoadmap = await screen.findByText(/Ask your coach/i);
    expect(noRoadmap).toBeTruthy();
  });

  it('renders skill nodes when roadmap data is available', async () => {
    vi.spyOn(client, 'fetchRoadmap').mockResolvedValue({
      userId: 'user-lea',
      profile: 2, // DotNet
      nodes: [
        {
          skillId: 10,
          skillName: 'C# Fundamentals',
          category: 'Language',
          levelCount: 5,
          currentNiveau: 2,
          targetNiveau: 4,
          status: 'Active',
          prerequisiteWarnings: [],
        },
        {
          skillId: 11,
          skillName: 'ASP.NET Core Web API',
          category: 'Framework',
          levelCount: 5,
          currentNiveau: 1,
          targetNiveau: 3,
          status: 'Active',
          prerequisiteWarnings: [],
        },
      ],
      totalSkillCount: 2,
      showAll: false,
    });
    vi.spyOn(client, 'fetchSeniorityProgress').mockResolvedValue({
      userId: 'user-lea',
      profile: 2,
      currentLevel: null,
      targetLevel: 1,
      metCount: 3,
      requiredCount: 6,
      unmetRequirements: [],
    });

    renderWithQuery(<Roadmap />);

    expect(await screen.findByText('C# Fundamentals')).toBeTruthy();
    expect(await screen.findByText('ASP.NET Core Web API')).toBeTruthy();
  });

  it('shows non-blocking prerequisite warning (FR8 / Story #17)', async () => {
    vi.spyOn(client, 'fetchRoadmap').mockResolvedValue({
      userId: 'user-lea',
      profile: 2,
      nodes: [
        {
          skillId: 15,
          skillName: 'Design Patterns',
          category: 'Architecture',
          levelCount: 4,
          currentNiveau: 0,
          targetNiveau: null,
          status: 'Locked',
          prerequisiteWarnings: [
            {
              skillId: 10,
              skillName: 'C# Fundamentals',
              requiredNiveau: 3,
              warningText:
                'C# Fundamentals niveau 3 not yet met — you can explore this skill, but your coach may ask you to address prerequisites first',
            },
          ],
        },
      ],
      totalSkillCount: 1,
      showAll: false,
    });
    vi.spyOn(client, 'fetchSeniorityProgress').mockResolvedValue({
      userId: 'user-lea',
      profile: 2,
      currentLevel: null,
      targetLevel: 1,
      metCount: 0,
      requiredCount: 6,
      unmetRequirements: [],
    });

    renderWithQuery(<Roadmap />);

    expect(await screen.findByText('Design Patterns')).toBeTruthy();
    // Warning toggle button should be visible — skill is NOT locked
    const warnBtn = await screen.findByText(/prerequisite warning/i);
    expect(warnBtn).toBeTruthy();
  });

  it('shows seniority progress indicator (FR39 / Story #35)', async () => {
    vi.spyOn(client, 'fetchRoadmap').mockResolvedValue({
      userId: 'user-lea',
      profile: 2,
      nodes: [],
      totalSkillCount: 0,
      showAll: false,
    });
    vi.spyOn(client, 'fetchSeniorityProgress').mockResolvedValue({
      userId: 'user-lea',
      profile: 2,
      currentLevel: null,
      targetLevel: 2, // Medior
      metCount: 15,
      requiredCount: 18,
      unmetRequirements: [],
    });

    renderWithQuery(<Roadmap />);

    // Journey 1: "She's 15/18 toward Medior"
    const progressText = await screen.findByText(/15\/18/);
    expect(progressText).toBeTruthy();
    expect(await screen.findByText(/Medior/i)).toBeTruthy();
  });
});
