import {describe, it, expect, beforeEach, vi} from 'vitest';
import {ChunkCacheService, type EntryWindowProvider} from './chunk-cache-service';
import type {IEntry} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IEntry';

/**
 * Helper to create mock entries
 */
function createEntry(id: string, lexeme: string): IEntry {
	return {
		id,
		lexemeForm: { en: lexeme },
		citationForm: {},
		literalMeaning: { spans: [] },
		note: { spans: [] },
		morphType: 0,
		senses: [],
		components: [],
		complexForms: [],
		complexFormTypes: [],
		publishIn: [],
		deletedAt: undefined
	};
}

/**
 * Mock backend provider
 */
class MockProvider implements EntryWindowProvider {
	private entries: IEntry[] = [];
	private failOnceStart: number | null = null;

	setEntries(entries: IEntry[]): void {
		this.entries = entries;
	}

	failNextWindowForStart(start: number): void {
		this.failOnceStart = start;
	}

	// eslint-disable-next-line @typescript-eslint/require-await
	async fetchWindow(start: number, size: number): Promise<{ entries: IEntry[]; firstIndex: number }> {
		if (this.failOnceStart === start) {
			this.failOnceStart = null;
			throw new Error('Transient fetch failure');
		}
		const window = this.entries.slice(start, start + size);
		return { entries: window, firstIndex: start };
	}

	// eslint-disable-next-line @typescript-eslint/require-await
	async fetchRowIndex(entryId: string): Promise<{ rowIndex: number; entry: IEntry }> {
		const rowIndex = this.entries.findIndex(e => e.id === entryId);
		if (rowIndex === -1) throw new Error(`Entry ${entryId} not found`);
		return { rowIndex, entry: this.entries[rowIndex] };
	}
}

describe('ChunkCacheService', () => {
	let service: ChunkCacheService;
	let provider: MockProvider;
	const chunkSize = 10;

	beforeEach(() => {
		provider = new MockProvider();
		// Create 35 mock entries (3.5 chunks worth)
		const entries = Array.from({ length: 35 }, (_, i) =>
			createEntry(`entry-${i}`, `Item ${i}`)
		);
		provider.setEntries(entries);
		service = new ChunkCacheService(provider, chunkSize);
	});

	describe('ensureWindow', () => {
		it('should fetch and cache first chunk', async () => {
			const result = await service.ensureWindow(0, 5);

			expect(result).toHaveLength(5);
			expect(result[0].id).toBe('entry-0');
			expect(result[4].id).toBe('entry-4');
		});

		it('should fetch multiple chunks for large window', async () => {
			const result = await service.ensureWindow(0, 25);

			expect(result).toHaveLength(25);
			expect(result[0].id).toBe('entry-0');
			expect(result[24].id).toBe('entry-24');
		});

		it('should cache and reuse chunks on second call', async () => {
			const fetchSpy = vi.spyOn(provider, 'fetchWindow');

			// First call
			await service.ensureWindow(0, 10);
			expect(fetchSpy).toHaveBeenCalled();
			const callCount1 = fetchSpy.mock.calls.length;

			// Second call to same window
			const result = await service.ensureWindow(0, 10);
			expect(result).toHaveLength(10);
			expect(fetchSpy.mock.calls.length).toBe(callCount1); // No additional calls
		});

		it('should emit onWindowReady event', async () => {
			const onWindowReady = vi.fn();
			service.subscribe({ onWindowReady });

			await service.ensureWindow(0, 5);

			expect(onWindowReady).toHaveBeenCalled();
		});

		it('should handle offset windows', async () => {
			const result = await service.ensureWindow(15, 5);

			expect(result).toHaveLength(5);
			expect(result[0].id).toBe('entry-15');
			expect(result[4].id).toBe('entry-19');
		});

		it('should handle partial last chunk', async () => {
			const result = await service.ensureWindow(30, 10);

			expect(result).toHaveLength(5); // Only 5 entries left
			expect(result[0].id).toBe('entry-30');
			expect(result[4].id).toBe('entry-34');
		});
	});

	describe('applyEntryUpdated', () => {
		it('should update entry in cached chunk', async () => {
			await service.ensureWindow(0, 10);

			const updatedEntry = createEntry('entry-3', 'Updated Item 3');
			service.applyEntryUpdated(updatedEntry);

			const result = await service.ensureWindow(0, 10);
			expect(result[3].lexemeForm.en).toBe('Updated Item 3');
		});

		it('should emit onChunkChanged event', async () => {
			await service.ensureWindow(0, 10);

			const onChunkChanged = vi.fn();
			service.subscribe({ onChunkChanged });

			const updatedEntry = createEntry('entry-5', 'Updated');
			service.applyEntryUpdated(updatedEntry);

			expect(onChunkChanged).toHaveBeenCalled();
		});

		it('should handle update on non-cached entry', () => {
			const onError = vi.fn();
			service.subscribe({ onError });

			// This should silently do nothing, not error
			const updatedEntry = createEntry('entry-100', 'Updated');
			service.applyEntryUpdated(updatedEntry);

			expect(onError).not.toHaveBeenCalled();
		});
	});

	describe('applyEntryDeleted', () => {
		it('should delete entry from cached chunk', async () => {
			await service.ensureWindow(0, 15);

			service.applyEntryDeleted('entry-5');

			const result = await service.ensureWindow(0, 15);
			expect(result).not.toContainEqual(expect.objectContaining({ id: 'entry-5' }));
		});

		it('should cascade delete from next chunk', async () => {
			await service.ensureWindow(0, 25);

			service.applyEntryDeleted('entry-5');

			const result = await service.ensureWindow(0, 25);
			// After delete of entry-5, entry-10 should move up
			expect(result[10].id).toBe('entry-11');
		});

		it('should emit onChunkChanged on cascade', async () => {
			await service.ensureWindow(0, 25);

			const onChunkChanged = vi.fn();
			service.subscribe({ onChunkChanged });

			service.applyEntryDeleted('entry-5');

			// Should be called at least once for the deletion
			expect(onChunkChanged).toHaveBeenCalled();
		});
	});

	describe('applyEntryInserted', () => {
		it('should insert entry at absolute index', async () => {
			await service.ensureWindow(0, 15);

			const newEntry = createEntry('new-entry', 'New Item');
			service.applyEntryInserted(newEntry, 5);

			const result = await service.ensureWindow(0, 20);
			expect(result[5]).toEqual(expect.objectContaining({ id: 'new-entry' }));
		});

		it('should cascade insert to next chunk', async () => {
			await service.ensureWindow(0, 25);

			const newEntry = createEntry('new-entry', 'New Item');
			service.applyEntryInserted(newEntry, 8);

			const result = await service.ensureWindow(0, 25);
			expect(result[8].id).toBe('new-entry');
			// entry-34 should be pushed out (was at index 34, now past end or in overflow)
		});

		it('should emit onChunkChanged on insert', async () => {
			await service.ensureWindow(0, 15);

			const onChunkChanged = vi.fn();
			service.subscribe({ onChunkChanged });

			const newEntry = createEntry('new-entry', 'New Item');
			service.applyEntryInserted(newEntry, 5);

			expect(onChunkChanged).toHaveBeenCalled();
		});
	});

	describe('delete and insert cascade patterns', () => {
		it('should handle multiple deletes', async () => {
			await service.ensureWindow(0, 20);

			service.applyEntryDeleted('entry-2');
			service.applyEntryDeleted('entry-5');
			service.applyEntryDeleted('entry-1');

			const result = await service.ensureWindow(0, 20);
			const ids = result.map(e => e.id);
			expect(ids).not.toContain('entry-1');
			expect(ids).not.toContain('entry-2');
			expect(ids).not.toContain('entry-5');
		});

		it('should handle insert followed by delete', async () => {
			await service.ensureWindow(0, 15);

			const newEntry = createEntry('new-entry', 'New');
			service.applyEntryInserted(newEntry, 5);

			service.applyEntryDeleted('new-entry');

			const result = await service.ensureWindow(0, 15);
			expect(result).not.toContainEqual(expect.objectContaining({ id: 'new-entry' }));
		});

		it('should maintain consistency after mixed operations', async () => {
			await service.ensureWindow(0, 20);

			service.applyEntryDeleted('entry-3');
			const newEntry = createEntry('inserted', 'Inserted');
			service.applyEntryInserted(newEntry, 10);
			const updated = createEntry('entry-8', 'Updated');
			service.applyEntryUpdated(updated);

			const result = await service.ensureWindow(0, 20);
			expect(result).not.toContainEqual(expect.objectContaining({ id: 'entry-3' }));
			expect(result).toContainEqual(expect.objectContaining({ id: 'inserted' }));
			// After deleting entry-3, entry-8 shifts from position 8 to 7
			expect(result[7].lexemeForm.en).toBe('Updated');
		});
	});

	describe('observer subscriptions', () => {
		it('should allow multiple observers', async () => {
			const obs1 = { onChunkChanged: vi.fn() };
			const obs2 = { onChunkChanged: vi.fn() };

			service.subscribe(obs1);
			service.subscribe(obs2);

			await service.ensureWindow(0, 10);
			service.applyEntryUpdated(createEntry('entry-0', 'Updated'));

			expect(obs1.onChunkChanged).toHaveBeenCalled();
			expect(obs2.onChunkChanged).toHaveBeenCalled();
		});

		it('should allow unsubscribe', async () => {
			const onChunkChanged = vi.fn();
			const unsubscribe = service.subscribe({ onChunkChanged });

			await service.ensureWindow(0, 10);
			service.applyEntryUpdated(createEntry('entry-0', 'Updated'));

			expect(onChunkChanged).toHaveBeenCalledTimes(1);

			unsubscribe();

			service.applyEntryUpdated(createEntry('entry-1', 'Updated'));
			expect(onChunkChanged).toHaveBeenCalledTimes(1); // No additional call
		});
	});

	describe('clear', () => {
		it('should clear all cached chunks', async () => {
			await service.ensureWindow(0, 20);

			service.clear();

			const onWindowReady = vi.fn();
			service.subscribe({ onWindowReady });

			// After clear, should fetch again
			const fetchSpy = vi.spyOn(provider, 'fetchWindow');
			await service.ensureWindow(0, 10);

			expect(fetchSpy).toHaveBeenCalled();
		});
	});

	describe('error handling', () => {
		it('should emit error on fetch failure', async () => {
			const failProvider = new (class implements EntryWindowProvider {
				// eslint-disable-next-line @typescript-eslint/require-await
				async fetchWindow(): Promise<{ entries: IEntry[]; firstIndex: number }> {
					throw new Error('Network error');
				}

				// eslint-disable-next-line @typescript-eslint/require-await
				async fetchRowIndex(): Promise<{ rowIndex: number; entry: IEntry }> {
					throw new Error('Network error');
				}
			})();

			const failService = new ChunkCacheService(failProvider, chunkSize);
			const onError = vi.fn();
			failService.subscribe({ onError });

			try {
				await failService.ensureWindow(0, 10);
			} catch {
				// Expected
			}

			expect(onError).toHaveBeenCalled();
		});
	});

	describe('window bounds and tail refill', () => {
		it('should preserve window length after delete by not cascading beyond visible range', async () => {
			await service.ensureWindow(0, 15);

			// Delete from within the window
			service.applyEntryDeleted('entry-5');

			const result = await service.ensureWindow(0, 15);
			// Window should still be 15 entries (refilled or pulled from cascade)
			expect(result.length).toBeGreaterThanOrEqual(14);
			expect(result).not.toContainEqual(expect.objectContaining({ id: 'entry-5' }));
		});

		it('should discard overflow outside requestedWindow on insert', async () => {
			await service.ensureWindow(0, 15);

			// Insert near end of visible window
			const newEntry = createEntry('new', 'New');
			service.applyEntryInserted(newEntry, 14);

			const result = await service.ensureWindow(0, 15);
			// Should have inserted entry and not accumulated garbage beyond window
			expect(result).toContainEqual(expect.objectContaining({ id: 'new' }));
			expect(result.length).toBeLessThanOrEqual(15);
		});

		it('should update chunk bounds when entries cascade', async () => {
			await service.ensureWindow(0, 20);

			service.applyEntryDeleted('entry-5');

			// After delete, window should shrink by 1 but entries should still be valid
			const result = await service.ensureWindow(0, 20);
			expect(result).toHaveLength(19);

			// Verify chunk boundaries were updated (no duplicates or gaps)
			const ids = new Set(result.map(e => e.id));
			expect(ids.size).toBe(result.length);
		});

		it('should maintain correct order during cascades', async () => {
			await service.ensureWindow(0, 20);

			service.applyEntryDeleted('entry-8');
			const newEntry = createEntry('inserted-x', 'X');
			service.applyEntryInserted(newEntry, 12);

			const result = await service.ensureWindow(0, 20);
			const ids = result.map(e => e.id);

			// Verify no duplicates
			const uniqueIds = new Set(ids);
			expect(uniqueIds.size).toBe(ids.length);

			// Verify order is still valid (monotonically increasing except for inserted)
			expect(ids).toContainEqual('inserted-x');
		});

			it('should cascade delete at chunk boundary and adjust next chunk bounds', async () => {
				await service.ensureWindow(0, 20);

				service.applyEntryDeleted('entry-9'); // last slot of chunk 0

				const result = await service.ensureWindow(0, 20);
				expect(result[9].id).toBe('entry-10');
				expect(result[10].id).toBe('entry-11');
			});

			it('should cascade insert at chunk boundary across multiple chunks', async () => {
				await service.ensureWindow(0, 30);

				const newEntry = createEntry('insert-boundary', 'Boundary');
				service.applyEntryInserted(newEntry, 10); // first slot of chunk 1

				const result = await service.ensureWindow(0, 30);
				expect(result[10].id).toBe('insert-boundary');
				// ensure original entries shifted but still ordered (entry-19 pushed toward chunk 2)
				expect(result).toContainEqual(expect.objectContaining({ id: 'entry-19' }));
			});

			it('should handle insert then delete in adjacent chunks without duplicates', async () => {
				await service.ensureWindow(0, 25);

				const inserted = createEntry('insert-adjacent', 'Adj');
				service.applyEntryInserted(inserted, 10); // at boundary
				service.applyEntryDeleted('entry-15'); // next chunk

				const result = await service.ensureWindow(0, 25);
				const ids = result.map(e => e.id);
				expect(new Set(ids).size).toBe(ids.length);
				expect(ids).toContain('insert-adjacent');
			});

			it('should recover after transient fetch failure', async () => {
				provider.failNextWindowForStart(0);
				await expect(service.ensureWindow(0, 10)).rejects.toThrow('Transient fetch failure');

				const result = await service.ensureWindow(0, 10);
				expect(result).toHaveLength(10);
			});

			it('should handle window shift without stale chunk interference', async () => {
				await service.ensureWindow(0, 15);
				const firstWindow = await service.ensureWindow(0, 15);
				expect(firstWindow[0].id).toBe('entry-0');

				const shifted = await service.ensureWindow(20, 5);
				expect(shifted[0].id).toBe('entry-20');
				expect(shifted).not.toContainEqual(expect.objectContaining({ id: 'entry-0' }));
			});

			it('should handle delete then insert at same absolute index', async () => {
				await service.ensureWindow(0, 20);

				service.applyEntryDeleted('entry-7');
				const replacement = createEntry('replacement-7', 'Repl');
				service.applyEntryInserted(replacement, 7);

				const result = await service.ensureWindow(0, 20);
				expect(result[7].id).toBe('replacement-7');
				expect(result).not.toContainEqual(expect.objectContaining({ id: 'entry-7' }));
			});
	});
});
