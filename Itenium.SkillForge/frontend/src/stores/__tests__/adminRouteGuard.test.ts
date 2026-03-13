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

describe('admin route guard (issue #13)', () => {
  it('backoffice user is allowed to access admin routes', () => {
    const token = createToken({
      sub: 'admin-1',
      name: 'Admin',
      email: 'admin@example.com',
      role: 'backoffice',
    });
    useAuthStore.getState().setToken(token);

    const { user } = useAuthStore.getState();
    expect(user?.isBackOffice).toBe(true);
  });

  it('manager user is NOT allowed to access admin routes', () => {
    const token = createToken({
      sub: 'manager-1',
      name: 'Manager',
      email: 'manager@example.com',
      role: 'manager',
    });
    useAuthStore.getState().setToken(token);

    const { user } = useAuthStore.getState();
    expect(user?.isBackOffice).toBe(false);
  });

  it('learner user is NOT allowed to access admin routes', () => {
    const token = createToken({
      sub: 'learner-1',
      name: 'Learner',
      email: 'learner@example.com',
      role: 'learner',
    });
    useAuthStore.getState().setToken(token);

    const { user } = useAuthStore.getState();
    expect(user?.isBackOffice).toBe(false);
  });

  it('unauthenticated user is NOT allowed to access admin routes', () => {
    const { user } = useAuthStore.getState();
    expect(user?.isBackOffice).toBeUndefined();
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
  });

  it('admin route guard redirects when isBackOffice is false', () => {
    // Simulates the guard logic: if (!user?.isBackOffice) => redirect
    const token = createToken({
      sub: 'manager-2',
      name: 'Coach',
      email: 'coach@example.com',
      role: 'manager',
    });
    useAuthStore.getState().setToken(token);

    const { user } = useAuthStore.getState();
    const shouldRedirect = !user?.isBackOffice;
    expect(shouldRedirect).toBe(true);
  });

  it('admin route guard does NOT redirect when isBackOffice is true', () => {
    const token = createToken({
      sub: 'admin-2',
      name: 'Admin',
      email: 'admin2@example.com',
      role: 'backoffice',
    });
    useAuthStore.getState().setToken(token);

    const { user } = useAuthStore.getState();
    const shouldRedirect = !user?.isBackOffice;
    expect(shouldRedirect).toBe(false);
  });
});
