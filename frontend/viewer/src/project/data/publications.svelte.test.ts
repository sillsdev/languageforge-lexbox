import {describe, expect, it} from 'vitest';
import {resolveMainPublication} from './publications.svelte';

describe('resolveMainPublication', () => {
  it('returns publication with isMain=true', () => {
    const main = {id: 'a', name: {}, isMain: true} as any;
    const notMain = {id: 'b', name: {}, isMain: false} as any;
    const noFlag = {id: 'c', name: {}} as any;

    expect(resolveMainPublication([notMain, main, noFlag])?.id).toBe('a');
  });

  it('returns undefined when no publication is main', () => {
    const notMain = {id: 'a', name: {}, isMain: false} as any;
    expect(resolveMainPublication([notMain])).toBeUndefined();
  });
});
