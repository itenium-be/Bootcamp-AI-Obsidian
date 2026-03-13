import { describe, it, expect, vi, beforeEach } from 'vitest';

/**
 * Story #25-#28: Resource Library API client unit tests.
 * Tests the request shape and response handling for resource endpoints.
 */

// Mock axios to test request formation without actual HTTP calls
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

// Re-import after mocking
const { fetchResources, createResource, rateResource, completeResource } = await import('@/api/client');

beforeEach(() => {
  vi.clearAllMocks();
});

describe('Resource Library API', () => {
  describe('fetchResources', () => {
    it('calls GET /api/resources without params when no filters', async () => {
      mockGet.mockResolvedValue({ data: [] });

      await fetchResources();

      expect(mockGet).toHaveBeenCalledWith('/api/resources', { params: {} });
    });

    it('includes skillId param when provided', async () => {
      mockGet.mockResolvedValue({ data: [] });

      await fetchResources({ skillId: 5 });

      expect(mockGet).toHaveBeenCalledWith('/api/resources', {
        params: { skillId: '5' },
      });
    });

    it('includes fromNiveau and toNiveau params when provided', async () => {
      mockGet.mockResolvedValue({ data: [] });

      await fetchResources({ fromNiveau: 2, toNiveau: 4 });

      expect(mockGet).toHaveBeenCalledWith('/api/resources', {
        params: { fromNiveau: '2', toNiveau: '4' },
      });
    });

    it('returns resource array from response', async () => {
      const mockResources = [
        { id: 'abc', title: 'Clean Code', url: 'http://x.com', type: 'Book', skillId: 1, fromNiveau: 2, toNiveau: 4, contributedBy: 'u1', contributedAt: '2026-01-01', thumbsUp: 5, thumbsDown: 1 },
      ];
      mockGet.mockResolvedValue({ data: mockResources });

      const result = await fetchResources();

      expect(result).toEqual(mockResources);
    });
  });

  describe('createResource', () => {
    it('calls POST /api/resources with payload', async () => {
      const payload = { title: 'Test', url: 'http://t.com', type: 1, skillId: 2, fromNiveau: 1, toNiveau: 3 };
      mockPost.mockResolvedValue({ data: { ...payload, id: 'new-id', contributedBy: 'user', contributedAt: '2026-01-01', thumbsUp: 0, thumbsDown: 0 } });

      await createResource(payload);

      expect(mockPost).toHaveBeenCalledWith('/api/resources', payload);
    });
  });

  describe('rateResource', () => {
    it('calls POST /api/resources/{id}/rate with up rating', async () => {
      mockPost.mockResolvedValue({ data: { thumbsUp: 6, thumbsDown: 1 } });

      const result = await rateResource('resource-id', 'up');

      expect(mockPost).toHaveBeenCalledWith('/api/resources/resource-id/rate', { rating: 'up' });
      expect(result.thumbsUp).toBe(6);
    });

    it('calls POST /api/resources/{id}/rate with down rating', async () => {
      mockPost.mockResolvedValue({ data: { thumbsUp: 5, thumbsDown: 2 } });

      await rateResource('resource-id', 'down');

      expect(mockPost).toHaveBeenCalledWith('/api/resources/resource-id/rate', { rating: 'down' });
    });
  });

  describe('completeResource', () => {
    it('calls POST /api/resources/{id}/complete with goalId', async () => {
      const completion = { id: 'comp-1', resourceId: 'res-1', consultantId: 'c1', goalId: 'goal-1', completedAt: '2026-01-01' };
      mockPost.mockResolvedValue({ data: completion });

      const result = await completeResource('res-1', 'goal-1');

      expect(mockPost).toHaveBeenCalledWith('/api/resources/res-1/complete', { goalId: 'goal-1' });
      expect(result.goalId).toBe('goal-1');
    });
  });
});
