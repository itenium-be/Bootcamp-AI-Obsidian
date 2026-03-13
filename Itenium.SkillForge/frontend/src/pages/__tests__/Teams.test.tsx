import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import { vi } from 'vitest';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: { language: 'en' },
  }),
}));

const mockUseQuery = vi.fn();
const mockUseMutation = vi.fn();
const mockUseQueryClient = vi.fn(() => ({ invalidateQueries: vi.fn() }));
vi.mock('@tanstack/react-query', () => ({
  useQuery: (...args: unknown[]) => mockUseQuery(...args),
  useMutation: (...args: unknown[]) => mockUseMutation(...args),
  useQueryClient: () => mockUseQueryClient(),
}));

vi.mock('sonner', () => ({ toast: { success: vi.fn(), error: vi.fn() } }));

vi.mock('@/api/client', () => ({
  fetchUserTeams: vi.fn(),
  fetchTeamMembers: vi.fn(),
  fetchUsers: vi.fn(),
  addTeamMember: vi.fn(),
  removeTeamMember: vi.fn(),
}));

vi.mock('lucide-react', () => ({
  UserPlus: () => <span data-testid="user-plus-icon" />,
  UserMinus: () => <span data-testid="user-minus-icon" />,
}));

vi.mock('@itenium-forge/ui', () => {
  const S = ({ children }: { children?: React.ReactNode }) => <div>{children}</div>;
  return {
    Button: ({ children, onClick, disabled }: React.ButtonHTMLAttributes<HTMLButtonElement>) => (
      <button onClick={onClick} disabled={disabled}>
        {children}
      </button>
    ),
    Select: ({ children, onValueChange }: { children: React.ReactNode; onValueChange?: (v: string) => void }) => (
      <div onClick={() => onValueChange?.('user-2')}>{children}</div>
    ),
    SelectTrigger: S,
    SelectContent: S,
    SelectItem: ({ children, value }: { children: React.ReactNode; value: string }) => (
      <option value={value}>{children}</option>
    ),
    SelectValue: () => null,
  };
});

import { Teams } from '../Teams';

const mockMutate = vi.fn();

const teams = [
  { id: 1, name: 'Java' },
  { id: 2, name: '.NET' },
];

const members = [{ id: 'u1', email: 'alice@test.com', firstName: 'Alice', lastName: 'Smith' }];

beforeEach(() => {
  vi.clearAllMocks();
  mockUseQuery.mockImplementation(({ queryKey }: { queryKey: unknown[] }) => {
    if (Array.isArray(queryKey) && queryKey[0] === 'teams') return { data: teams, isLoading: false };
    if (Array.isArray(queryKey) && queryKey[0] === 'team-members') return { data: members, isLoading: false };
    if (Array.isArray(queryKey) && queryKey[0] === 'users') return { data: [], isLoading: false };
    return { data: [], isLoading: false };
  });
  mockUseMutation.mockReturnValue({ mutate: mockMutate, isPending: false });
});

describe('Teams', () => {
  it('renders the page title', () => {
    render(<Teams />);
    expect(screen.getByText('teams.title')).toBeInTheDocument();
  });

  it('renders team tabs', () => {
    render(<Teams />);
    expect(screen.getByText('Java')).toBeInTheDocument();
    expect(screen.getByText('.NET')).toBeInTheDocument();
  });

  it('shows the first team as active by default', () => {
    render(<Teams />);
    expect(screen.getByText('Alice Smith')).toBeInTheDocument();
  });

  it('shows member email', () => {
    render(<Teams />);
    expect(screen.getByText('alice@test.com')).toBeInTheDocument();
  });

  it('shows no members message when team is empty', () => {
    mockUseQuery.mockImplementation(({ queryKey }: { queryKey: unknown[] }) => {
      if (Array.isArray(queryKey) && queryKey[0] === 'teams') return { data: teams, isLoading: false };
      if (Array.isArray(queryKey) && queryKey[0] === 'team-members') return { data: [], isLoading: false };
      if (Array.isArray(queryKey) && queryKey[0] === 'users') return { data: [], isLoading: false };
      return { data: [], isLoading: false };
    });
    render(<Teams />);
    expect(screen.getByText('teams.noMembers')).toBeInTheDocument();
  });

  it('switches teams when a tab is clicked', async () => {
    mockUseQuery.mockImplementation(({ queryKey }: { queryKey: unknown[] }) => {
      if (Array.isArray(queryKey) && queryKey[0] === 'teams') return { data: teams, isLoading: false };
      if (Array.isArray(queryKey) && queryKey[0] === 'team-members' && queryKey[1] === 2)
        return { data: [], isLoading: false };
      if (Array.isArray(queryKey) && queryKey[0] === 'team-members') return { data: members, isLoading: false };
      if (Array.isArray(queryKey) && queryKey[0] === 'users') return { data: [], isLoading: false };
      return { data: [], isLoading: false };
    });
    render(<Teams />);
    expect(screen.getByText('Alice Smith')).toBeInTheDocument();
    fireEvent.click(screen.getByText('.NET'));
    await waitFor(() => {
      expect(screen.queryByText('Alice Smith')).not.toBeInTheDocument();
    });
  });

  it('calls remove mutation when remove button is clicked', () => {
    render(<Teams />);
    fireEvent.click(screen.getByText('teams.removeMember'));
    expect(mockMutate).toHaveBeenCalled();
  });

  it('calls add mutation when add button is clicked after selecting a user', async () => {
    mockUseQuery.mockImplementation(({ queryKey }: { queryKey: unknown[] }) => {
      if (Array.isArray(queryKey) && queryKey[0] === 'teams') return { data: teams, isLoading: false };
      if (Array.isArray(queryKey) && queryKey[0] === 'team-members') return { data: [], isLoading: false };
      if (Array.isArray(queryKey) && queryKey[0] === 'users')
        return {
          data: [{ id: 'user-2', email: 'bob@test.com', firstName: 'Bob', lastName: 'Jones', roles: ['learner'] }],
          isLoading: false,
        };
      return { data: [], isLoading: false };
    });
    render(<Teams />);

    // Click the SelectItem for Bob to trigger onValueChange (bubbles up to Select mock)
    fireEvent.click(screen.getByText('Bob Jones (bob@test.com)'));

    await waitFor(() => {
      expect(screen.getByText('teams.addMember')).not.toBeDisabled();
    });

    fireEvent.click(screen.getByText('teams.addMember'));
    expect(mockMutate).toHaveBeenCalled();
  });
});
