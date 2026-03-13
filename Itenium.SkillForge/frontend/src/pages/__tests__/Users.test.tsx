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
vi.mock('@tanstack/react-query', () => ({
  useQuery: (...args: unknown[]) => mockUseQuery(...args),
  useMutation: (...args: unknown[]) => mockUseMutation(...args),
}));

vi.mock('sonner', () => ({ toast: { success: vi.fn(), error: vi.fn() } }));

vi.mock('@/api/client', () => ({
  fetchUserTeams: vi.fn(),
  createUser: vi.fn(),
}));

vi.mock('lucide-react', () => ({
  Loader2: () => <span data-testid="loader" />,
  UserPlus: () => <span data-testid="user-plus-icon" />,
}));

vi.mock('@itenium-forge/ui', () => {
  const S = ({ children }: { children?: React.ReactNode }) => <div>{children}</div>;
  return {
    Button: ({ children, onClick, type, disabled }: React.ButtonHTMLAttributes<HTMLButtonElement>) => (
      <button onClick={onClick} type={type} disabled={disabled}>
        {children}
      </button>
    ),
    Input: ({ placeholder, ...props }: React.InputHTMLAttributes<HTMLInputElement>) => (
      <input placeholder={placeholder} {...props} />
    ),
    Form: S,
    FormField: ({ render: renderFn }: { render: (props: { field: object }) => React.ReactNode }) =>
      renderFn({ field: { value: '', onChange: vi.fn(), onBlur: vi.fn(), ref: vi.fn(), name: '' } }),
    FormItem: S,
    FormLabel: ({ children }: { children: React.ReactNode }) => <label>{children}</label>,
    FormControl: S,
    FormMessage: () => null,
    Card: S,
    CardHeader: S,
    CardTitle: ({ children }: { children: React.ReactNode }) => <h2>{children}</h2>,
    CardContent: S,
    CardFooter: S,
    Select: ({ children, onValueChange }: { children: React.ReactNode; onValueChange?: (v: string) => void }) => (
      <div onClick={() => onValueChange?.('manager')}>{children}</div>
    ),
    SelectTrigger: S,
    SelectContent: S,
    SelectItem: ({ children, value }: { children: React.ReactNode; value: string }) => (
      <option value={value}>{children}</option>
    ),
    SelectValue: () => null,
    Checkbox: ({ onCheckedChange }: { onCheckedChange?: (v: boolean) => void }) => (
      <input type="checkbox" onChange={(e) => onCheckedChange?.(e.target.checked)} />
    ),
  };
});

import { Users } from '../Users';

const mockMutate = vi.fn();

beforeEach(() => {
  mockUseQuery.mockReturnValue({ data: [{ id: 1, name: 'Java' }] });
  mockUseMutation.mockReturnValue({ mutate: mockMutate, isPending: false });
  vi.clearAllMocks();
  mockUseMutation.mockReturnValue({ mutate: mockMutate, isPending: false });
});

describe('Users', () => {
  it('renders the page title', () => {
    render(<Users />);
    expect(screen.getByText('users.title')).toBeInTheDocument();
  });

  it('shows the create user button initially', () => {
    render(<Users />);
    expect(screen.getByText('users.createUser')).toBeInTheDocument();
  });

  it('shows the form when create user button is clicked', () => {
    render(<Users />);
    fireEvent.click(screen.getByText('users.createUser'));
    expect(screen.getByText('common.save')).toBeInTheDocument();
    expect(screen.getByText('common.cancel')).toBeInTheDocument();
  });

  it('hides the form when cancel is clicked', async () => {
    render(<Users />);
    fireEvent.click(screen.getByText('users.createUser'));
    fireEvent.click(screen.getByText('common.cancel'));
    await waitFor(() => {
      expect(screen.queryByText('common.save')).not.toBeInTheDocument();
    });
  });

  it('hides the create button while the form is open', () => {
    render(<Users />);
    fireEvent.click(screen.getByText('users.createUser'));
    // The header button is gone; only the card title remains
    expect(screen.queryByTestId('user-plus-icon')).not.toBeInTheDocument();
  });
});
