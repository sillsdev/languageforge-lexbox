import { logger } from '@papi/frontend';
import { DEBOUNCE_CANCELED_ERROR_MESSAGE, debounce, getErrorMessage } from 'platform-bible-utils';
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';

/** How long a scheduled lookup waits, so typing does not query the backend per keystroke. */
const SCHEDULED_LOOKUP_DELAY_MS = 500;

/** One lookup to run, and what to do with its answer. */
export type EntryLookupRequest<T> = {
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
  /** Stands every lookup down, scheduled or in flight, and returns to idle. */
  reset: () => void;
  /** Starts a lookup now, in place of anything scheduled or in flight. */
  run: <T>(lookup: EntryLookupRequest<T>) => void;
  /**
   * Waits out {@link SCHEDULED_LOOKUP_DELAY_MS} and then starts the lookup `resolve` returns,
   * discarding whatever was scheduled or in flight before. Pending from the moment it is called, so
   * a view does not present what it holds as the answer to a query still being waited on.
   *
   * `resolve` runs when the wait is over, so it reads the state of that moment, and returning
   * `undefined` starts nothing.
   */
  schedule: <T>(resolve: () => EntryLookupRequest<T> | undefined) => void;
};

type EntryLookup = {
  didFail: boolean;
  isPending: boolean;
  /** Stable across renders, so a callback that depends on it is not remade on each one. */
  lookup: EntryLookupActions;
};

/** Reports a lookup answer that a view could not take, which only a bug in its handlers causes. */
function logUnexpected(e: unknown): void {
  logger.error('Error handling a lookup answer:', e);
}

/** Swallows the rejection `cancel` raises, since standing a lookup down is not a failure. */
function ignoreCancellation(e: unknown): void {
  if (getErrorMessage(e) !== DEBOUNCE_CANCELED_ERROR_MESSAGE)
    logger.error('Scheduled lookup failed:', e);
}

/**
 * Keeps a view's pending and failure state tied to the newest of its entry lookups.
 *
 * A stale query must never update the view. This hook keeps only the newest scheduled or in-flight
 * lookup active.
 */
export default function useEntryLookup(): EntryLookup {
  const [didFail, setDidFail] = useState(false);
  const [isPending, setIsPending] = useState(false);
  const newestRef = useRef(0);

  const nextId = useCallback(() => {
    newestRef.current += 1;
    return newestRef.current;
  }, []);

  const perform = useCallback(
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

  // One waiting slot for the whole view: scheduling again replaces what was waiting.
  const waitThenStart = useMemo(
    () => debounce((start: () => void) => start(), SCHEDULED_LOOKUP_DELAY_MS),
    [],
  );

  useEffect(() => () => waitThenStart.cancel(), [waitThenStart]);

  const reset = useCallback((): void => {
    nextId();
    waitThenStart.cancel();
    setDidFail(false);
    setIsPending(false);
  }, [nextId, waitThenStart]);

  const run = useCallback(
    <T>(lookup: EntryLookupRequest<T>): void => {
      waitThenStart.cancel();
      perform(lookup).catch(logUnexpected);
    },
    [perform, waitThenStart],
  );

  const schedule = useCallback(
    <T>(resolve: () => EntryLookupRequest<T> | undefined): void => {
      // Answers owed to the query being replaced are no longer wanted, even before this starts.
      const id = nextId();
      setDidFail(false);
      setIsPending(true);
      waitThenStart(() => {
        if (id !== newestRef.current) return;
        const lookup = resolve();
        if (lookup) perform(lookup).catch(logUnexpected);
        // The wait was the whole of it, so nothing else will report that it is over.
        else setIsPending(false);
      }).catch(ignoreCancellation);
    },
    [nextId, perform, waitThenStart],
  );

  const lookup = useMemo(() => ({ reset, run, schedule }), [reset, run, schedule]);

  return { didFail, isPending, lookup };
}
