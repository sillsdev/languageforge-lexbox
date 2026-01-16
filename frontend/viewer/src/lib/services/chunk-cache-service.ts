import type {IEntry} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IEntry';

/**
 * Represents a fixed-size chunk of entries in the cache.
 * Each chunk stores entries for a contiguous range of indices.
 */
interface Chunk {
	/** Starting index of this chunk (0-based) */
	start: number;
	/** Ending index (exclusive) of this chunk */
	endExclusive: number;
	/** Cached entries in this chunk */
	entries: IEntry[];
	/** Current status: 'ready', 'loading', or 'stale' */
	status: 'ready' | 'loading' | 'stale';
}

/**
 * Callback for observing chunk cache changes.
 */
export interface ChunkCacheObserver {
	/**
	 * Called when a chunk is updated (entry inserted/updated/deleted).
	 * index: the chunk index in the cache map
	 */
	onChunkChanged?: (chunkIndex: number) => void;
	/**
	 * Called when a new window of entries is ready (e.g., after ensureWindow completes).
	 */
	onWindowReady?: () => void;
	/**
	 * Called when an error occurs.
	 */
	onError?: (error: Error) => void;
}

/**
 * Interface for the backend API that provides entry windows and row indices.
 * This is typically implemented by an HTTP client talking to MiniLcm endpoints.
 */
export interface EntryWindowProvider {
	/**
	 * Fetch a window of entries.
	 * start: 0-based offset
	 * size: number of entries to fetch
	 * Returns: { entries, firstIndex } where firstIndex is the 0-based start
	 */
	fetchWindow(start: number, size: number): Promise<{ entries: IEntry[]; firstIndex: number }>;

	/**
	 * Get the 0-based row index of a specific entry.
	 * Typically called during jump-to-entry flow.
	 */
	fetchRowIndex(entryId: string): Promise<{ rowIndex: number; entry: IEntry }>;
}

/**
 * ChunkCacheService manages chunk-based virtual scrolling.
 *
 * Responsibilities:
 * - Maintain a map of chunks keyed by chunk index
 * - Support ensureWindow(start, size) to fetch and cache entry windows
 * - Apply entry updates/deletes/inserts with cascade rebalancing
 * - Emit observable events for Svelte reactivity
 *
 * Chunk math:
 * - Chunks are fixed-size (except the tail)
 * - Chunk index = Math.floor(startIndex / chunkSize)
 * - When deleting/inserting, cascade between chunks to maintain alignment
 */
export class ChunkCacheService {
	private chunks = new Map<number, Chunk>();
	private requestedWindow: { start: number; size: number } | null = null;
	private pendingWindows = new Map<number, Promise<void>>();
	private observers: ChunkCacheObserver[] = [];
	private isLoading = false;

	constructor(
		private provider: EntryWindowProvider,
		private chunkSize: number = 50
	) {}

	/**
	 * Subscribe to cache change events.
	 * Returns an unsubscribe function.
	 */
	subscribe(observer: ChunkCacheObserver): () => void {
		this.observers.push(observer);
		return () => {
			const idx = this.observers.indexOf(observer);
			if (idx >= 0) this.observers.splice(idx, 1);
		};
	}

	/**
	 * Ensure that a window of entries is loaded and ready.
	 * - If chunks are already cached, returns immediately
	 * - Otherwise, fetches from backend and fills chunks
	 * - Emits onWindowReady when complete
	 */
	async ensureWindow(start: number, size: number): Promise<IEntry[]> {
		this.requestedWindow = { start, size };

		const chunkIndices = this.getChunkIndicesForWindow(start, size);
		const missingChunks = chunkIndices.filter(idx => !this.chunks.has(idx));

		if (missingChunks.length === 0) {
			// All chunks already cached, return immediately
			return this.extractWindow(start, size);
		}

		// Fetch missing chunks
		for (const chunkIdx of missingChunks) {
			// Avoid duplicate fetches by using a promise cache
			if (!this.pendingWindows.has(chunkIdx)) {
				const promise = this.fetchAndCacheChunk(chunkIdx);
				this.pendingWindows.set(chunkIdx, promise);
				void promise
					.then(() => {
						this.pendingWindows.delete(chunkIdx);
					})
					.catch((err: unknown) => {
						this.pendingWindows.delete(chunkIdx);
						this.notifyError(err instanceof Error ? err : new Error(String(err)));
					});
			}

			// Wait for this chunk to be fetched
			await this.pendingWindows.get(chunkIdx)!;
		}

		// All chunks are now ready
		this.notifyWindowReady();
		return this.extractWindow(start, size);
	}

	/**
	 * Apply an entry update (modification to existing entry).
	 * If the entry is cached, updates it in-place and emits onChunkChanged.
	 */
	applyEntryUpdated(entry: IEntry): void {
		for (const [chunkIdx, chunk] of this.chunks) {
			const idx = chunk.entries.findIndex(e => e.id === entry.id);
			if (idx >= 0) {
				chunk.entries[idx] = entry;
				this.notifyChunkChanged(chunkIdx);
				return;
			}
		}
	}

	/**
	 * Apply an entry deletion.
	 * - Removes entry from cache
	 * - Cascades deletions to subsequent chunks (squeezes left)
	 * - If cascade reaches a chunk outside the requested window, marks it stale
	 * - May trigger a tail refill if visible chunk becomes short
	 */
	applyEntryDeleted(entryId: string): void {
		for (const [chunkIdx, chunk] of this.chunks) {
			const idx = chunk.entries.findIndex(e => e.id === entryId);
			if (idx >= 0) {
				// Found the entry, remove it
				chunk.entries.splice(idx, 1);
				this.notifyChunkChanged(chunkIdx);

				// Cascade: pull entries from subsequent chunks
				this.cascadeDelete(chunkIdx);
				return;
			}
		}
	}

	/**
	 * Apply an entry insertion at an absolute index.
	 * - Inserts entry into the owning chunk
	 * - Cascades the last element to the next chunk
	 * - Continues cascade until a chunk outside requested window, then discards
	 */
	applyEntryInserted(entry: IEntry, absoluteIndex: number): void {
		const chunkIdx = Math.floor(absoluteIndex / this.chunkSize);
		const offsetInChunk = absoluteIndex % this.chunkSize;

		// Ensure the chunk exists
		if (!this.chunks.has(chunkIdx)) {
			this.chunks.set(chunkIdx, {
				start: chunkIdx * this.chunkSize,
				endExclusive: (chunkIdx + 1) * this.chunkSize,
				entries: [],
				status: 'ready'
			});
		}

		const chunk = this.chunks.get(chunkIdx)!;
		chunk.entries.splice(offsetInChunk, 0, entry);
		this.notifyChunkChanged(chunkIdx);

		// Cascade: overflow pushes to next chunk
		this.cascadeInsert(chunkIdx);
	}

	/**
	 * Clear all cached chunks and reset state.
	 */
	clear(): void {
		this.chunks.clear();
		this.requestedWindow = null;
		this.pendingWindows.clear();
		this.isLoading = false;
	}

	// ===== PRIVATE HELPERS =====

	/**
	 * Get the chunk indices that cover the requested window.
	 */
	private getChunkIndicesForWindow(start: number, size: number): number[] {
		const end = start + size;
		const startChunk = Math.floor(start / this.chunkSize);
		const endChunk = Math.floor((end - 1) / this.chunkSize) + 1;

		const indices: number[] = [];
		for (let i = startChunk; i < endChunk; i++) {
			indices.push(i);
		}
		return indices;
	}

	/**
	 * Fetch a single chunk from the backend and cache it.
	 */
	private async fetchAndCacheChunk(chunkIdx: number, chunkStartOverride?: number): Promise<void> {
		const chunkStart = chunkStartOverride ?? chunkIdx * this.chunkSize;

		// Mark as loading
		const chunk: Chunk = {
			start: chunkStart,
			endExclusive: chunkStart + this.chunkSize,
			entries: [],
			status: 'loading'
		};
		this.chunks.set(chunkIdx, chunk);

		try {
			const { entries, firstIndex } = await this.provider.fetchWindow(chunkStart, this.chunkSize);
			chunk.entries = entries;
			chunk.status = 'ready';
			// Align chunk boundaries based on actual firstIndex
			chunk.start = firstIndex;
			chunk.endExclusive = firstIndex + entries.length;
			// Don't notify on initial fetch - only notify when data is explicitly modified
		} catch (err) {
			// Drop errored chunk so callers treat this range as uncached and retry later
			this.chunks.delete(chunkIdx);
			throw err;
		}
	}

	/**
	 * Extract a window from cached chunks.
	 * Concatenates entries from relevant chunks in deterministic chunk-index order,
	 * accounting for chunk start positions.
	 */
	private extractWindow(start: number, size: number): IEntry[] {
		const result: IEntry[] = [];
		const endIndex = start + size;

		// Get chunk indices that should cover this window
		const chunkIndices = this.getChunkIndicesForWindow(start, size);

		// Iterate in deterministic chunk-index order to ensure consistent output
		for (const chunkIdx of chunkIndices) {
			const chunk = this.chunks.get(chunkIdx);
			if (!chunk) {
				// Gap in chunks - window is incomplete
				break;
			}

			// Skip chunks that are completely before or after the window
			if (chunk.endExclusive <= start || chunk.start >= endIndex) {
				continue;
			}

			// Calculate the slice range within this chunk
			const chunkStart = Math.max(0, start - chunk.start);
			const chunkEnd = Math.min(chunk.entries.length, endIndex - chunk.start);

			for (let i = chunkStart; i < chunkEnd; i++) {
				result.push(chunk.entries[i]);
			}
		}

		return result;
	}

	/**
	 * Cascade deletions: when an entry is deleted, the first entry from the next chunk
	 * moves into this chunk's empty slot. Updates chunk bounds and marks tail stale if needed.
	 */
	private cascadeDelete(chunkIdx: number): void {
		const nextChunkIdx = chunkIdx + 1;
		const nextChunk = this.chunks.get(nextChunkIdx);

		if (!nextChunk || nextChunk.entries.length === 0) {
			// No more entries to pull from next chunk
			// Check if this chunk is the tail and is inside the visible window
			const chunk = this.chunks.get(chunkIdx)!;
			if (this.isChunkVisible(chunk) && chunk.entries.length < this.chunkSize && nextChunk) {
				// Visible tail chunk is now short - mark stale and refill
				chunk.status = 'stale';
				this.refillVisibleTail(chunkIdx);
			}
			return;
		}

		// Pull the first entry from the next chunk into this chunk
		const entryFromNext = nextChunk.entries.shift()!;
		const chunk = this.chunks.get(chunkIdx)!;
		chunk.entries.push(entryFromNext);

		// Update next chunk's start position (entries shifted left)
		nextChunk.start++;
		nextChunk.endExclusive = nextChunk.start + nextChunk.entries.length;

		this.notifyChunkChanged(nextChunkIdx);

		// Continue cascading
		this.cascadeDelete(nextChunkIdx);
	}

	/**
	 * Cascade inserts: when an entry is inserted, the last entry overflows to the next chunk.
	 * Discards overflow outside the requestedWindow to avoid accumulating stale data.
	 * Updates chunk bounds for entries that moved.
	 */
	private cascadeInsert(chunkIdx: number): void {
		const chunk = this.chunks.get(chunkIdx)!;

		if (chunk.entries.length <= this.chunkSize) {
			// Chunk is not oversized, no cascade needed
			return;
		}

		// Pop the last entry and push to next chunk
		const overflowEntry = chunk.entries.pop()!;

		const nextChunkIdx = chunkIdx + 1;
		let nextChunk = this.chunks.get(nextChunkIdx);

		if (!nextChunk) {
			// Check if next chunk is outside the visible window
			if (this.requestedWindow && (nextChunkIdx * this.chunkSize) >= this.requestedWindow.start + this.requestedWindow.size) {
				// Overflow outside window - discard it
				return;
			}
			// Create next chunk if it doesn't exist and is inside window
			nextChunk = {
				start: nextChunkIdx * this.chunkSize,
				endExclusive: (nextChunkIdx + 1) * this.chunkSize,
				entries: [],
				status: 'ready'
			};
			this.chunks.set(nextChunkIdx, nextChunk);
		}

		nextChunk.entries.unshift(overflowEntry);
		// Update next chunk's start position (entries shifted right)
		nextChunk.start = nextChunkIdx * this.chunkSize;
		nextChunk.endExclusive = nextChunk.start + nextChunk.entries.length;

		this.notifyChunkChanged(nextChunkIdx);

		// Recursively cascade if next chunk is also oversized
		this.cascadeInsert(nextChunkIdx);
	}

	// ===== TAIL REFILL =====

	/**
	 * Check if a chunk overlaps with the currently requested window.
	 */
	private isChunkVisible(chunk: Chunk): boolean {
		if (!this.requestedWindow) return false;
		const windowEnd = this.requestedWindow.start + this.requestedWindow.size;
		return !(chunk.endExclusive <= this.requestedWindow.start || chunk.start >= windowEnd);
	}

	/**
	 * When a visible tail chunk becomes short after a delete, refill it from the backend.
	 */
	private refillVisibleTail(chunkIdx: number): void {
		const chunk = this.chunks.get(chunkIdx);
		if (!chunk) return;

		// Fire off a refill fetch for this chunk
		const promise = this.fetchAndCacheChunk(chunkIdx, chunk.start)
			.catch((err: unknown) => {
				this.notifyError(err instanceof Error ? err : new Error(String(err)));
			});
		this.pendingWindows.set(chunkIdx, promise);
		void promise.finally(() => {
			this.pendingWindows.delete(chunkIdx);
		});
	}

	// ===== NOTIFICATIONS =====

	private notifyChunkChanged(chunkIdx: number): void {
		this.observers.forEach(obs => obs.onChunkChanged?.(chunkIdx));
	}

	private notifyWindowReady(): void {
		this.observers.forEach(obs => obs.onWindowReady?.());
	}

	private notifyError(error: Error): void {
		this.observers.forEach(obs => obs.onError?.(error));
	}
}
