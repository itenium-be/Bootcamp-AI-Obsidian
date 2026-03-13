import { describe, it, expect, vi, beforeEach } from 'vitest';

/**
 * Stories #31-#33: Live Session API client unit tests.
 */

const mockGet = vi.fn();
const mockPost = vi.fn();
const mockPut = vi.fn();

vi.mock('axios', async (importOriginal) => {
  const actual = await importOriginal<typeof import('axios')>();
  return {
    ...actual,
    default: {
      ...actual.default,
      create: () => ({
        get: mockGet,
        post: mockPost,
        put: mockPut,
        interceptors: {
          request: { use: vi.fn() },
          response: { use: vi.fn() },
        },
      }),
      post: vi.fn(),
    },
  };
});

const { startSession, getSessionFocus, endSession, updateSessionNotes, createValidation } = await import('@/api/client');

beforeEach(() => {
  vi.clearAllMocks();
});

describe('Session API', () => {
  describe('startSession', () => {
    it('calls POST /api/sessions with consultantId', async () => {
      const session = { id: 'sess-1', coachId: 'coach', consultantId: 'lea', startedAt: '2026-01-01', endedAt: null, notes: null };
      mockPost.mockResolvedValue({ data: session });

      const result = await startSession('lea');

      expect(mockPost).toHaveBeenCalledWith('/api/sessions', { consultantId: 'lea' });
      expect(result.id).toBe('sess-1');
    });
  });

  describe('getSessionFocus', () => {
    it('calls GET /api/sessions/{id}/focus', async () => {
      const focus = { sessionId: 'sess-1', consultantId: 'lea', startedAt: '2026-01-01', activeGoals: [], pendingReadinessFlags: [] };
      mockGet.mockResolvedValue({ data: focus });

      const result = await getSessionFocus('sess-1');

      expect(mockGet).toHaveBeenCalledWith('/api/sessions/sess-1/focus');
      expect(result.sessionId).toBe('sess-1');
    });
  });

  describe('endSession', () => {
    it('calls PUT /api/sessions/{id}/end', async () => {
      const session = { id: 'sess-1', coachId: 'coach', consultantId: 'lea', startedAt: '2026-01-01', endedAt: '2026-01-01T10:00:00', notes: null };
      mockPut.mockResolvedValue({ data: session });

      const result = await endSession('sess-1');

      expect(mockPut).toHaveBeenCalledWith('/api/sessions/sess-1/end');
      expect(result.endedAt).not.toBeNull();
    });
  });

  describe('updateSessionNotes', () => {
    it('calls PUT /api/sessions/{id}/notes with notes', async () => {
      const session = { id: 'sess-1', coachId: 'coach', consultantId: 'lea', startedAt: '2026-01-01', endedAt: null, notes: 'Strong grasp of naming.' };
      mockPut.mockResolvedValue({ data: session });

      const result = await updateSessionNotes('sess-1', 'Strong grasp of naming.');

      expect(mockPut).toHaveBeenCalledWith('/api/sessions/sess-1/notes', { notes: 'Strong grasp of naming.' });
      expect(result.notes).toBe('Strong grasp of naming.');
    });
  });

  describe('createValidation', () => {
    it('calls POST /api/validations with validation data', async () => {
      const validation = { id: 'val-1', skillId: 3, consultantId: 'lea', validatedBy: 'coach', validatedAt: '2026-01-01', fromNiveau: 1, toNiveau: 2, sessionId: 'sess-1', notes: null };
      mockPost.mockResolvedValue({ data: validation });

      const result = await createValidation({
        skillId: 3,
        consultantId: 'lea',
        fromNiveau: 1,
        toNiveau: 2,
        sessionId: 'sess-1',
        notes: null,
      });

      expect(mockPost).toHaveBeenCalledWith('/api/validations', {
        skillId: 3,
        consultantId: 'lea',
        fromNiveau: 1,
        toNiveau: 2,
        sessionId: 'sess-1',
        notes: null,
      });
      expect(result.toNiveau).toBe(2);
    });
  });
});
