import {describe, expect, it} from 'vitest';
import {resolveDefaultPublication} from './publications.svelte';

describe('resolveDefaultPublication', () => {
  it('returns publication with isMain=true', () => {
    const main = {id: 'a', name: {}, isMain: true} as any;
    const notMain = {id: 'b', name: {}, isMain: false} as any;
    const noFlag = {id: 'c', name: {}} as any;

    expect(resolveDefaultPublication([notMain, main, noFlag])?.id).toBe('a');
  });

  it('returns undefined when no publication is main', () => {
    const notMain = {id: 'a', name: {}, isMain: false} as any;
    expect(resolveDefaultPublication([notMain])).toBeUndefined();
  });
});
