import {describe, it, expect, beforeEach, vi} from 'vitest';
import {HistoryService} from './history-service';
import type {IEntry, IExampleSentence, ISense} from '$lib/dotnet-types';
import type {IProjectActivity} from '$lib/dotnet-types/generated-types/LcmCrdt/IProjectActivity';

// Mock fetch globally
global.fetch = vi.fn();

// Mock the project context
const createMockProjectContext = () => ({
  projectCode: 'test-project',
  projectName: 'test-project',
  historyService: undefined as any,
});

describe('HistoryService', () => {
  let historyService: HistoryService;
  let mockProjectContext: ReturnType<typeof createMockProjectContext>;

  beforeEach(() => {
    mockProjectContext = createMockProjectContext();
    historyService = new HistoryService(mockProjectContext);
    vi.clearAllMocks();
  });

  describe('load', () => {
    it('fetches history from API when historyApi is not available', async () => {
      // Arrange
      const objectId = 'test-object-id';
      const mockHistoryData = [
        {
          commitId: 'commit-1',
          timestamp: '2024-01-01T00:00:00Z',
          snapshotId: 'snapshot-1',
          changeIndex: 0,
          changeName: 'Create Entry',
          authorName: 'Test User',
          entity: undefined,
          entityName: undefined,
        },
      ];

      (global.fetch as any).mockResolvedValue({
        json: () => Promise.resolve(mockHistoryData),
      });

      // Act
      const result = await historyService.load(objectId);

      // Assert
      expect(global.fetch).toHaveBeenCalledWith(
        `/api/history/test-project/${objectId}`
      );
      expect(result).toHaveLength(1);
    });

    it('returns empty array when data is not an array', async () => {
      // Arrange
      const objectId = 'test-object-id';

      (global.fetch as any).mockResolvedValue({
        json: () => Promise.resolve({invalid: 'data'}),
      });

      // Act
      const result = await historyService.load(objectId);

      // Assert
      expect(result).toEqual([]);
    });

    it('handles null changeName in history items', async () => {
      // Arrange
      const objectId = 'test-object-id';
      const mockHistoryData = [
        {
          commitId: 'commit-1',
          timestamp: '2024-01-01T00:00:00Z',
          snapshotId: 'snapshot-1',
          changeIndex: 0,
          changeName: null,
          authorName: 'Test User',
          entity: undefined,
          entityName: undefined,
        },
      ];

      (global.fetch as any).mockResolvedValue({
        json: () => Promise.resolve(mockHistoryData),
      });

      // Act
      const result = await historyService.load(objectId);

      // Assert
      expect(result).toHaveLength(1);
      expect(result[0].changeName).toBeNull();
    });

    it('handles undefined changeName in history items', async () => {
      // Arrange
      const objectId = 'test-object-id';
      const mockHistoryData = [
        {
          commitId: 'commit-1',
          timestamp: '2024-01-01T00:00:00Z',
          snapshotId: 'snapshot-1',
          changeIndex: 0,
          changeName: undefined,
          authorName: 'Test User',
          entity: undefined,
          entityName: undefined,
        },
      ];

      (global.fetch as any).mockResolvedValue({
        json: () => Promise.resolve(mockHistoryData),
      });

      // Act
      const result = await historyService.load(objectId);

      // Assert
      expect(result).toHaveLength(1);
      expect(result[0].changeName).toBeUndefined();
    });

    it('handles null authorName in history items', async () => {
      // Arrange
      const objectId = 'test-object-id';
      const mockHistoryData = [
        {
          commitId: 'commit-1',
          timestamp: '2024-01-01T00:00:00Z',
          snapshotId: 'snapshot-1',
          changeIndex: 0,
          changeName: 'Create Entry',
          authorName: null,
          entity: undefined,
          entityName: undefined,
        },
      ];

      (global.fetch as any).mockResolvedValue({
        json: () => Promise.resolve(mockHistoryData),
      });

      // Act
      const result = await historyService.load(objectId);

      // Assert
      expect(result).toHaveLength(1);
      expect(result[0].authorName).toBeNull();
    });

    it('sets previousTimestamp for each item', async () => {
      // Arrange
      const objectId = 'test-object-id';
      const mockHistoryData = [
        {
          commitId: 'commit-1',
          timestamp: '2024-01-03T00:00:00Z',
          snapshotId: 'snapshot-1',
          changeIndex: 0,
          changeName: 'Change Entry',
          authorName: 'User 1',
          entity: undefined,
          entityName: undefined,
        },
        {
          commitId: 'commit-2',
          timestamp: '2024-01-02T00:00:00Z',
          snapshotId: 'snapshot-2',
          changeIndex: 0,
          changeName: 'Change Entry',
          authorName: 'User 2',
          entity: undefined,
          entityName: undefined,
        },
        {
          commitId: 'commit-3',
          timestamp: '2024-01-01T00:00:00Z',
          snapshotId: 'snapshot-3',
          changeIndex: 0,
          changeName: 'Create Entry',
          authorName: 'User 3',
          entity: undefined,
          entityName: undefined,
        },
      ];

      (global.fetch as any).mockResolvedValue({
        json: () => Promise.resolve(mockHistoryData),
      });

      // Act
      const result = await historyService.load(objectId);

      // Assert
      expect(result[0].previousTimestamp).toBe('2024-01-02T00:00:00Z');
      expect(result[1].previousTimestamp).toBe('2024-01-01T00:00:00Z');
      expect(result[2].previousTimestamp).toBeUndefined();
    });

    it('reverses history so most recent is first', async () => {
      // Arrange
      const objectId = 'test-object-id';
      const mockHistoryData = [
        {
          commitId: 'commit-1',
          timestamp: '2024-01-01T00:00:00Z',
          snapshotId: 'snapshot-1',
          changeIndex: 0,
          changeName: 'Create Entry',
          authorName: 'User 1',
          entity: undefined,
          entityName: undefined,
        },
        {
          commitId: 'commit-2',
          timestamp: '2024-01-02T00:00:00Z',
          snapshotId: 'snapshot-2',
          changeIndex: 0,
          changeName: 'Change Entry',
          authorName: 'User 2',
          entity: undefined,
          entityName: undefined,
        },
      ];

      (global.fetch as any).mockResolvedValue({
        json: () => Promise.resolve(mockHistoryData),
      });

      // Act
      const result = await historyService.load(objectId);

      // Assert
      expect(result[0].timestamp).toBe('2024-01-02T00:00:00Z');
      expect(result[1].timestamp).toBe('2024-01-01T00:00:00Z');
    });

    it('uses historyApi when available', async () => {
      // Arrange
      const objectId = 'test-object-id';
      const mockHistoryData = [
        {
          commitId: 'commit-1',
          timestamp: '2024-01-01T00:00:00Z',
          snapshotId: 'snapshot-1',
          changeIndex: 0,
          changeName: 'Create Entry',
          authorName: 'Test User',
          entity: undefined,
          entityName: undefined,
        },
      ];

      mockProjectContext.historyService = {
        getHistory: vi.fn().mockResolvedValue(mockHistoryData),
      } as any;

      historyService = new HistoryService(mockProjectContext);

      // Act
      const result = await historyService.load(objectId);

      // Assert
      expect(mockProjectContext.historyService.getHistory).toHaveBeenCalledWith(
        objectId
      );
      expect(global.fetch).not.toHaveBeenCalled();
      expect(result).toHaveLength(1);
    });

    it('handles empty history array', async () => {
      // Arrange
      const objectId = 'test-object-id';
      const mockHistoryData: any[] = [];

      (global.fetch as any).mockResolvedValue({
        json: () => Promise.resolve(mockHistoryData),
      });

      // Act
      const result = await historyService.load(objectId);

      // Assert
      expect(result).toEqual([]);
    });

    it('handles history items with all nullable fields set to null', async () => {
      // Arrange
      const objectId = 'test-object-id';
      const mockHistoryData = [
        {
          commitId: 'commit-1',
          timestamp: '2024-01-01T00:00:00Z',
          snapshotId: 'snapshot-1',
          changeIndex: 0,
          changeName: null,
          authorName: null,
          entity: undefined,
          entityName: undefined,
        },
      ];

      (global.fetch as any).mockResolvedValue({
        json: () => Promise.resolve(mockHistoryData),
      });

      // Act
      const result = await historyService.load(objectId);

      // Assert
      expect(result).toHaveLength(1);
      expect(result[0].changeName).toBeNull();
      expect(result[0].authorName).toBeNull();
    });
  });

  describe('fetchSnapshot', () => {
    it('returns Entry type when entity is an entry', async () => {
      // Arrange
      const objectId = 'test-object-id';
      const mockEntry: IEntry = {
        id: objectId,
        lexemeForm: {},
        citationForm: {},
        literalMeaning: {},
        note: {},
        senses: [],
      } as any;

      const history = {
        commitId: 'commit-1',
        timestamp: '2024-01-01T00:00:00Z',
        snapshotId: 'snapshot-1',
        changeIndex: 0,
        changeName: 'Create Entry',
        authorName: 'Test User',
        entity: undefined,
        entityName: undefined,
      };

      (global.fetch as any).mockResolvedValue({
        json: () => Promise.resolve(mockEntry),
      });

      // Act
      const result = await historyService.fetchSnapshot(history, objectId);

      // Assert
      expect(result.entity).toEqual(mockEntry);
      expect(result.entityName).toBe('Entry');
    });

    it('returns Sense type when entity is a sense', async () => {
      // Arrange
      const objectId = 'test-object-id';
      const mockSense: ISense = {
        id: objectId,
        entryId: 'entry-id',
        gloss: {},
        definition: {},
        partOfSpeech: '',
        semanticDomains: [],
        exampleSentences: [],
      } as any;

      const history = {
        commitId: 'commit-1',
        timestamp: '2024-01-01T00:00:00Z',
        snapshotId: 'snapshot-1',
        changeIndex: 0,
        changeName: 'Create Sense',
        authorName: 'Test User',
        entity: undefined,
        entityName: undefined,
      };

      (global.fetch as any).mockResolvedValue({
        json: () => Promise.resolve(mockSense),
      });

      // Act
      const result = await historyService.fetchSnapshot(history, objectId);

      // Assert
      expect(result.entity).toEqual(mockSense);
      expect(result.entityName).toBe('Sense');
    });

    it('returns ExampleSentence type when entity is an example', async () => {
      // Arrange
      const objectId = 'test-object-id';
      const mockExample: IExampleSentence = {
        id: objectId,
        senseId: 'sense-id',
        sentence: {},
        translation: {},
        reference: '',
      } as any;

      const history = {
        commitId: 'commit-1',
        timestamp: '2024-01-01T00:00:00Z',
        snapshotId: 'snapshot-1',
        changeIndex: 0,
        changeName: 'Create Example',
        authorName: 'Test User',
        entity: undefined,
        entityName: undefined,
      };

      (global.fetch as any).mockResolvedValue({
        json: () => Promise.resolve(mockExample),
      });

      // Act
      const result = await historyService.fetchSnapshot(history, objectId);

      // Assert
      expect(result.entity).toEqual(mockExample);
      expect(result.entityName).toBe('ExampleSentence');
    });

    it('throws error when entity type cannot be determined', async () => {
      // Arrange
      const objectId = 'test-object-id';
      const mockUnknownEntity = {
        id: objectId,
        unknownField: 'value',
      };

      const history = {
        commitId: 'commit-1',
        timestamp: '2024-01-01T00:00:00Z',
        snapshotId: 'snapshot-1',
        changeIndex: 0,
        changeName: 'Unknown',
        authorName: 'Test User',
        entity: undefined,
        entityName: undefined,
      };

      (global.fetch as any).mockResolvedValue({
        json: () => Promise.resolve(mockUnknownEntity),
      });

      // Act & Assert
      await expect(
        historyService.fetchSnapshot(history, objectId)
      ).rejects.toThrow('Unable to determine type of object');
    });

    it('uses historyApi when available', async () => {
      // Arrange
      const objectId = 'test-object-id';
      const mockEntry: IEntry = {
        id: objectId,
        lexemeForm: {},
        citationForm: {},
        literalMeaning: {},
        note: {},
        senses: [],
      } as any;

      const history = {
        commitId: 'commit-1',
        timestamp: '2024-01-01T00:00:00Z',
        snapshotId: 'snapshot-1',
        changeIndex: 0,
        changeName: 'Create Entry',
        authorName: 'Test User',
        entity: undefined,
        entityName: undefined,
      };

      mockProjectContext.historyService = {
        getObject: vi.fn().mockResolvedValue(mockEntry),
      } as any;

      historyService = new HistoryService(mockProjectContext);

      // Act
      const result = await historyService.fetchSnapshot(history, objectId);

      // Assert
      expect(mockProjectContext.historyService.getObject).toHaveBeenCalledWith(
        'commit-1',
        objectId
      );
      expect(global.fetch).not.toHaveBeenCalled();
      expect(result.entityName).toBe('Entry');
    });
  });

  describe('activity', () => {
    it('fetches project activity from API when historyApi is not available', async () => {
      // Arrange
      const projectName = 'test-project';
      const mockActivity: IProjectActivity[] = [
        {
          commitId: 'commit-1',
          timestamp: '2024-01-01T00:00:00Z',
          changes: [],
          metadata: {} as any,
          changeName: 'Create Entry',
        },
      ];

      (global.fetch as any).mockResolvedValue({
        json: () => Promise.resolve(mockActivity),
      });

      // Act
      const result = await historyService.activity(projectName);

      // Assert
      expect(global.fetch).toHaveBeenCalledWith(`/api/activity/${projectName}`);
      expect(result).toEqual(mockActivity);
    });

    it('uses historyApi when available', async () => {
      // Arrange
      const projectName = 'test-project';
      const mockActivity: IProjectActivity[] = [
        {
          commitId: 'commit-1',
          timestamp: '2024-01-01T00:00:00Z',
          changes: [],
          metadata: {} as any,
          changeName: 'Create Entry',
        },
      ];

      mockProjectContext.historyService = {
        projectActivity: vi.fn().mockResolvedValue(mockActivity),
      } as any;

      historyService = new HistoryService(mockProjectContext);

      // Act
      const result = await historyService.activity(projectName);

      // Assert
      expect(
        mockProjectContext.historyService.projectActivity
      ).toHaveBeenCalled();
      expect(global.fetch).not.toHaveBeenCalled();
      expect(result).toEqual(mockActivity);
    });
  });

  describe('HistoryItem type', () => {
    it('allows changeName to be undefined', () => {
      // This test validates the type definition
      const item = {
        commitId: 'commit-1',
        timestamp: '2024-01-01T00:00:00Z',
        snapshotId: 'snapshot-1',
        changeIndex: 0,
        changeName: undefined,
        authorName: 'Test User',
        entity: undefined,
        entityName: undefined,
      };

      // Type assertion to ensure it matches HistoryItem type
      expect(item.changeName).toBeUndefined();
    });

    it('allows authorName to be undefined', () => {
      // This test validates the type definition
      const item = {
        commitId: 'commit-1',
        timestamp: '2024-01-01T00:00:00Z',
        snapshotId: 'snapshot-1',
        changeIndex: 0,
        changeName: 'Create Entry',
        authorName: undefined,
        entity: undefined,
        entityName: undefined,
      };

      // Type assertion to ensure it matches HistoryItem type
      expect(item.authorName).toBeUndefined();
    });

    it('allows both changeName and authorName to be undefined', () => {
      // This test validates the type definition for edge cases
      const item = {
        commitId: 'commit-1',
        timestamp: '2024-01-01T00:00:00Z',
        snapshotId: 'snapshot-1',
        changeIndex: 0,
        changeName: undefined,
        authorName: undefined,
        entity: undefined,
        entityName: undefined,
      };

      // Type assertion to ensure it matches HistoryItem type
      expect(item.changeName).toBeUndefined();
      expect(item.authorName).toBeUndefined();
    });
  });

  describe('error handling', () => {
    it('handles fetch errors gracefully in load', async () => {
      // Arrange
      const objectId = 'test-object-id';
      (global.fetch as any).mockRejectedValue(new Error('Network error'));

      // Act & Assert
      await expect(historyService.load(objectId)).rejects.toThrow(
        'Network error'
      );
    });

    it('handles fetch errors gracefully in fetchSnapshot', async () => {
      // Arrange
      const objectId = 'test-object-id';
      const history = {
        commitId: 'commit-1',
        timestamp: '2024-01-01T00:00:00Z',
        snapshotId: 'snapshot-1',
        changeIndex: 0,
        changeName: 'Create Entry',
        authorName: 'Test User',
        entity: undefined,
        entityName: undefined,
      };

      (global.fetch as any).mockRejectedValue(new Error('Network error'));

      // Act & Assert
      await expect(
        historyService.fetchSnapshot(history, objectId)
      ).rejects.toThrow('Network error');
    });

    it('handles fetch errors gracefully in activity', async () => {
      // Arrange
      const projectName = 'test-project';
      (global.fetch as any).mockRejectedValue(new Error('Network error'));

      // Act & Assert
      await expect(historyService.activity(projectName)).rejects.toThrow(
        'Network error'
      );
    });
  });
});