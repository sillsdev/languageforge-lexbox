import {describe, expect, it} from 'vitest';
import type {IPublication} from '$lib/dotnet-types';
import {resolveMainPublication} from './publications.svelte';

function publication(id: string, isMain: boolean): IPublication {
  return {id, name: {}, isMain};
}

describe('resolveMainPublication', () => {
  it('returns the publication with isMain=true', () => {
    const main = publication('a', true);

    expect(resolveMainPublication([publication('b', false), main, publication('c', false)])?.id).toBe('a');
  });

  it('returns undefined when no publication is main', () => {
    expect(resolveMainPublication([publication('a', false)])).toBeUndefined();
  });
});
