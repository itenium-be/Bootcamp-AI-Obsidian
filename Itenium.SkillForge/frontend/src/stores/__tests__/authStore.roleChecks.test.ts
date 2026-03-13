/**
 * Story #11/#12/#13: Auth role checks in the frontend store.
 * Verifies that the authStore correctly identifies backoffice, manager, and learner roles.
 */
import { useAuthStore } from '../authStore';

function createToken(payload: Record<string, unknown>): string {
  const header = btoa(JSON.stringify({ alg: 'none', typ: 'JWT' }));
  const body = btoa(JSON.stringify(payload));
  return `${header}.${body}.sig`;
}

function resetStore() {
  useAuthStore.setState({ accessToken: null, user: null, isAuthenticated: false });
  localStorage.clear();
}

beforeEach(() => {
  resetStore();
});

describe('Auth role checks (Stories #11/#12/#13)', () => {
  // Story #13: Admin (backoffice) gets platform-wide access
  it('identifies backoffice admin correctly', () => {
    const token = createToken({
      sub: 'admin-1',
      name: 'Admin User',
      email: 'admin@itenium.be',
      role: 'backoffice',
      exp: Math.floor(Date.now() / 1000) + 3600,
    });

    useAuthStore.getState().setToken(token);
    const user = useAuthStore.getState().user;

    expect(user?.isBackOffice).toBe(true);
  });

  // Story #12: Coach (manager) is NOT backoffice — cannot access admin views
  it('identifies manager as not backoffice', () => {
    const token = createToken({
      sub: 'coach-1',
      name: 'Java Coach',
      email: 'coach@itenium.be',
      role: 'manager',
      exp: Math.floor(Date.now() / 1000) + 3600,
    });

    useAuthStore.getState().setToken(token);
    const user = useAuthStore.getState().user;

    expect(user?.isBackOffice).toBe(false);
  });

  // Story #11: Consultant (learner) is NOT backoffice — restricted to own data
  it('identifies learner as not backoffice', () => {
    const token = createToken({
      sub: 'learner-1',
      name: 'Sander Dev',
      email: 'sander@itenium.be',
      role: 'learner',
      exp: Math.floor(Date.now() / 1000) + 3600,
    });

    useAuthStore.getState().setToken(token);
    const user = useAuthStore.getState().user;

    expect(user?.isBackOffice).toBe(false);
  });

  // Story #15: Admin creates user — verify admin guard works
  it('admin guard: isBackOffice true for backoffice role', () => {
    const token = createToken({
      sub: 'admin-2',
      role: 'backoffice',
      exp: Math.floor(Date.now() / 1000) + 3600,
    });

    useAuthStore.getState().setToken(token);

    const { user } = useAuthStore.getState();
    // Admin user management page at /admin/users is guarded by isBackOffice
    expect(user?.isBackOffice).toBe(true);
  });

  // Story #37: Only backoffice can restore — verify manager cannot
  it('restore guard: manager is not backoffice and cannot restore users', () => {
    const token = createToken({
      sub: 'manager-1',
      role: 'manager',
      exp: Math.floor(Date.now() / 1000) + 3600,
    });

    useAuthStore.getState().setToken(token);

    const { user } = useAuthStore.getState();
    // FR42: Only backoffice role can perform restoration
    expect(user?.isBackOffice).toBe(false);
  });
});
