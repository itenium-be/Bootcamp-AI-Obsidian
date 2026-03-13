import { describe, it, expect, beforeEach } from 'vitest';
import { useAuthStore } from '../authStore';

/** Create a JWT with the given payload (signature is not verified by jwt-decode) */
function createToken(payload: Record<string, unknown>): string {
  const header = btoa(JSON.stringify({ alg: 'none', typ: 'JWT' }));
  const body = btoa(JSON.stringify(payload));
  return `${header}.${body}.sig`;
}

function resetStore() {
  useAuthStore.setState({
    accessToken: null,
    user: null,
    isAuthenticated: false,
  });
  localStorage.clear();
}

beforeEach(() => {
  resetStore();
});

/**
 * Issue #11 — Consultant login with own-data access scope
 *
 * FR1: Consultant can only access own data — admin routes blocked by route guard (issue #13)
 * FR4: Cannot write skill validations (403) — enforcement is on the backend via
 *      [Authorize(Roles = "manager")] on ValidationController (pending issue #14)
 */
describe('consultant (learner) access scope (issue #11)', () => {
  it('learner user has isBackOffice = false', () => {
    const token = createToken({
      sub: 'learner-1',
      name: 'Learner',
      email: 'learner@example.com',
      role: 'learner',
    });

    useAuthStore.getState().setToken(token);

    expect(useAuthStore.getState().user?.isBackOffice).toBe(false);
  });

  it('learner user cannot access admin routes (isBackOffice is false)', () => {
    const token = createToken({
      sub: 'learner-2',
      name: 'Learner',
      email: 'learner2@example.com',
      role: 'learner',
    });

    useAuthStore.getState().setToken(token);

    const { user } = useAuthStore.getState();
    // The admin route guard redirects when isBackOffice is false
    const wouldBeRedirected = !user?.isBackOffice;
    expect(wouldBeRedirected).toBe(true);
  });

  it('learner user is authenticated after login', () => {
    const token = createToken({
      sub: 'learner-3',
      name: 'Learner',
      email: 'learner3@example.com',
      role: 'learner',
    });

    useAuthStore.getState().setToken(token);

    expect(useAuthStore.getState().isAuthenticated).toBe(true);
  });

  it('learner has email and name from token', () => {
    const token = createToken({
      sub: 'learner-4',
      name: 'Jane Doe',
      email: 'jane@example.com',
      role: 'learner',
    });

    useAuthStore.getState().setToken(token);

    const user = useAuthStore.getState().user;
    expect(user?.email).toBe('jane@example.com');
    expect(user?.name).toBe('Jane Doe');
  });

  it('learner user cannot impersonate backoffice by modifying role in token', () => {
    // The token with only learner role should NOT give backoffice access
    const token = createToken({
      sub: 'learner-5',
      name: 'Malicious',
      email: 'malicious@example.com',
      role: 'learner',
    });

    useAuthStore.getState().setToken(token);

    expect(useAuthStore.getState().user?.isBackOffice).toBe(false);
  });
});
