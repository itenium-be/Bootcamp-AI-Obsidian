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

describe('useAuthStore', () => {
  describe('setToken', () => {
    it('sets user from a token with all fields', () => {
      const token = createToken({
        sub: 'user-123',
        name: 'Alice',
        email: 'alice@example.com',
        role: 'backoffice',
        exp: Math.floor(Date.now() / 1000) + 3600,
      });

      useAuthStore.getState().setToken(token);

      const state = useAuthStore.getState();
      expect(state.isAuthenticated).toBe(true);
      expect(state.accessToken).toBe(token);
      expect(state.user).toEqual({
        id: 'user-123',
        name: 'Alice',
        email: 'alice@example.com',
        isBackOffice: true,
        isManager: false,
        role: 'backoffice',
      });
    });

    it('handles role as an array', () => {
      const token = createToken({
        sub: 'user-456',
        name: 'Bob',
        email: 'bob@example.com',
        role: ['manager', 'backoffice'],
      });

      useAuthStore.getState().setToken(token);

      expect(useAuthStore.getState().user?.isBackOffice).toBe(true);
    });

    it('sets isBackOffice to false when role does not include backoffice', () => {
      const token = createToken({
        sub: 'user-789',
        name: 'Charlie',
        email: 'charlie@example.com',
        role: 'manager',
      });

      useAuthStore.getState().setToken(token);

      expect(useAuthStore.getState().user?.isBackOffice).toBe(false);
    });

    it('sets isBackOffice to false when role is missing', () => {
      const token = createToken({
        sub: 'user-000',
        name: 'Dave',
        email: 'dave@example.com',
      });

      useAuthStore.getState().setToken(token);

      expect(useAuthStore.getState().user?.isBackOffice).toBe(false);
    });

    it('falls back to preferred_username for email and name', () => {
      const token = createToken({
        sub: 'user-111',
        preferred_username: 'jane.doe',
      });

      useAuthStore.getState().setToken(token);

      const user = useAuthStore.getState().user;
      expect(user?.email).toBe('jane.doe');
      expect(user?.name).toBe('jane.doe');
    });

    it('falls back to defaults when no name or username', () => {
      const token = createToken({
        sub: 'user-222',
      });

      useAuthStore.getState().setToken(token);

      const user = useAuthStore.getState().user;
      expect(user?.email).toBe('');
      expect(user?.name).toBe('User');
    });

    // --- Issue #12: Coach login with team-scoped access ---

    it('sets isManager to true for manager role', () => {
      const token = createToken({
        sub: 'manager-1',
        name: 'Coach',
        email: 'coach@example.com',
        role: 'manager',
      });

      useAuthStore.getState().setToken(token);

      expect(useAuthStore.getState().user?.isManager).toBe(true);
    });

    it('sets isManager to false for backoffice role', () => {
      const token = createToken({
        sub: 'admin-1',
        name: 'Admin',
        email: 'admin@example.com',
        role: 'backoffice',
      });

      useAuthStore.getState().setToken(token);

      expect(useAuthStore.getState().user?.isManager).toBe(false);
    });

    it('sets isManager to false for learner role', () => {
      const token = createToken({
        sub: 'learner-1',
        name: 'Learner',
        email: 'learner@example.com',
        role: 'learner',
      });

      useAuthStore.getState().setToken(token);

      expect(useAuthStore.getState().user?.isManager).toBe(false);
    });

    it('sets isManager to false when role is missing', () => {
      const token = createToken({
        sub: 'user-000',
        name: 'Dave',
        email: 'dave@example.com',
      });

      useAuthStore.getState().setToken(token);

      expect(useAuthStore.getState().user?.isManager).toBe(false);
    });

    it('sets role field from token', () => {
      const token = createToken({
        sub: 'manager-2',
        name: 'Coach',
        email: 'coach2@example.com',
        role: 'manager',
      });

      useAuthStore.getState().setToken(token);

      expect(useAuthStore.getState().user?.role).toBe('manager');
    });

    it('sets role to learner as default when role is missing', () => {
      const token = createToken({
        sub: 'user-no-role',
        name: 'No Role',
        email: 'norole@example.com',
      });

      useAuthStore.getState().setToken(token);

      expect(useAuthStore.getState().user?.role).toBe('learner');
    });

    it('sets role to first role when role is an array', () => {
      const token = createToken({
        sub: 'user-multi',
        name: 'Multi',
        email: 'multi@example.com',
        role: ['manager', 'backoffice'],
      });

      useAuthStore.getState().setToken(token);

      expect(useAuthStore.getState().user?.role).toBe('manager');
    });
  });

  describe('logout', () => {
    it('clears auth state', () => {
      const token = createToken({ sub: 'user-1', name: 'Test', email: 'test@test.com' });
      useAuthStore.getState().setToken(token);

      useAuthStore.getState().logout();

      const state = useAuthStore.getState();
      expect(state.isAuthenticated).toBe(false);
      expect(state.accessToken).toBeNull();
      expect(state.user).toBeNull();
    });

    it('removes team-storage from localStorage', () => {
      localStorage.setItem('team-storage', JSON.stringify({ mode: 'manager' }));

      useAuthStore.getState().logout();

      expect(localStorage.getItem('team-storage')).toBeNull();
    });
  });
});
