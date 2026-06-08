import {describe, expect, it} from 'vitest';
import {resolveDefaultPublication} from './publications.svelte';

describe('resolveDefaultPublication', () => {
  it('returns publication with latest defaultedAt', () => {
    const first = {id: 'a', name: {}, defaultedAt: '1970-01-01T00:00:00.0000000+00:00'} as any;
    const latest = {id: 'b', name: {}, defaultedAt: '1970-01-01T00:01:00.0000000+00:00'} as any;
    const none = {id: 'c', name: {}} as any;

    expect(resolveDefaultPublication([first, latest, none])?.id).toBe('b');
  });
});
