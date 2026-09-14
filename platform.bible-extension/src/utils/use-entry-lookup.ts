import { logger } from '@papi/frontend';
import { useCallback, useMemo, useRef, useState } from 'react';

/** One lookup to run, and what to do with its answer. */
type EntryLookupRequest<T> = {
  /** Prefixes the logged error when the request rejects. */
  failureMessage: string;
  /** Clears whatever the answer would have filled. Runs only for the newest lookup. */
  onFailure?: () => void;
  /** Runs only for the newest lookup. */
  onResult: (result: T) => void;
  request: () => Promise<T>;
};

/** The lookups a view can start. */
type EntryLookupActions = {
  /** Discards the answers of lookups in flight and returns to idle, for when none is coming. */
  reset: () => void;
  /** Runs one lookup, letting its answer reach the view only while it is the newest. */
  run: <T>(lookup: EntryLookupRequest<T>) => Promise<void>;
  /** Discards the answers of lookups in flight, staying pending for the one that follows. */
  supersede: () => void;
};

type EntryLookup = {
  didFail: boolean;
  isPending: boolean;
  /**
   * Stable across renders, so a callback that depends on it is not remade on each one — which would
   * also remake any debounced wrapper around that callback, dropping the call it holds.
   */
  lookup: EntryLookupActions;
};

/**
 * Keeps a view's pending and failure state tied to the newest of its entry lookups.
 *
 * Several lookups can be in flight at once, since debouncing spaces them out rather than
 * serializing them, and a query can change before the lookup answering it has even started. Each
 * lookup is numbered as it starts, and only the newest one's answer reaches the view.
 */
export default function useEntryLookup(): EntryLookup {
  const [didFail, setDidFail] = useState(false);
  const [isPending, setIsPending] = useState(false);
  const newestRef = useRef(0);

  const nextId = useCallback(() => {
    newestRef.current += 1;
    return newestRef.current;
  }, []);

  const supersede = useCallback((): void => {
    nextId();
  }, [nextId]);

  const reset = useCallback((): void => {
    nextId();
    setDidFail(false);
    setIsPending(false);
  }, [nextId]);

  const run = useCallback(
    async <T>({ failureMessage, onFailure, onResult, request }: EntryLookupRequest<T>) => {
      const id = nextId();
      setDidFail(false);
      setIsPending(true);
      try {
        const result = await request();
        if (id !== newestRef.current) return;
        onResult(result);
      } catch (e) {
        logger.error(failureMessage, e);
        if (id !== newestRef.current) return;
        onFailure?.();
        setDidFail(true);
      } finally {
        if (id === newestRef.current) setIsPending(false);
      }
    },
    [nextId],
  );

  const lookup = useMemo(() => ({ reset, run, supersede }), [reset, run, supersede]);

  return { didFail, isPending, lookup };
}
