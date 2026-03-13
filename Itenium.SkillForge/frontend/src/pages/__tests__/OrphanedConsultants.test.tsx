import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import { vi } from 'vitest';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: { language: 'en' },
  }),
}));

const mockUseQuery = vi.fn();
vi.mock('@tanstack/react-query', () => ({
  useQuery: (...args: unknown[]) => mockUseQuery(...args),
}));

vi.mock('@/api/client', () => ({
  fetchOrphanedConsultants: vi.fn(),
}));

import { OrphanedConsultants } from '../OrphanedConsultants';

beforeEach(() => {
  vi.clearAllMocks();
  mockUseQuery.mockReturnValue({ data: [], isLoading: false });
});

describe('OrphanedConsultants', () => {
  it('renders title', () => {
    render(<OrphanedConsultants />);
    expect(screen.getByText('users.orphaned.title')).toBeInTheDocument();
  });

  it('shows loading state', () => {
    mockUseQuery.mockReturnValue({ data: [], isLoading: true });
    render(<OrphanedConsultants />);
    expect(screen.getByText('common.loading')).toBeInTheDocument();
  });

  it('shows empty state when no orphaned consultants', () => {
    render(<OrphanedConsultants />);
    expect(screen.getByText('users.orphaned.noOrphaned')).toBeInTheDocument();
  });

  it('shows consultants in table', () => {
    mockUseQuery.mockReturnValue({
      data: [{ id: '1', email: 'a@test.com', firstName: 'Alice', lastName: 'Smith', roles: ['learner'] }],
      isLoading: false,
    });
    render(<OrphanedConsultants />);
    expect(screen.getByText('Alice Smith')).toBeInTheDocument();
    expect(screen.getByText('a@test.com')).toBeInTheDocument();
    expect(screen.getByText('learner')).toBeInTheDocument();
  });
});
