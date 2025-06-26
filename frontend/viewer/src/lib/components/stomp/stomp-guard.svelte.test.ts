import {afterEach, beforeEach, describe, expect, it} from 'vitest';

import {StompGuard} from './stomp-guard.svelte';
import {tick} from 'svelte';

describe('StompGuard', () => {

  let stompGuard: StompGuard<number>;
  let parentValue = $state(42);
  let cleanup: () => void;

  beforeEach(() => {
    cleanup = $effect.root(() => {
      stompGuard = new StompGuard(() => parentValue, (value) => parentValue = value);
    });
  });

  afterEach(() => {
    cleanup();
  });

  it('initializes with the parent value', () => {
    expect(stompGuard.value).toBe(parentValue);
  });

  it('accepts parent values when not dirty', () => {
    parentValue = 100; // parent change
    expect(stompGuard.value).toBe(100); // was accepted
  });

  it('pushs changes to parent', () => {
    stompGuard.value = 100; // make dirty
    expect(stompGuard.value).toBe(100);
    expect(parentValue).toBe(100); // parent was updated
  });

  it('ignores parent values when dirty', () => {
    stompGuard.value = 100; // make dirty
    expect(stompGuard.value).toBe(100);
    parentValue = 200; // parent change
    expect(stompGuard.value).toBe(100); // was ignored
  });

  it('reverts new parent values when ignored', async () => {
    stompGuard.value = 100; // make dirty
    parentValue = 200; // parent change
    expect(stompGuard.value).toBe(100); // was ignored
    await tick();
    expect(parentValue).toBe(100); // parent was reverted
  });

  it('starts accepting parent changes again after a flush', () => {
    stompGuard.value = 100; // make dirty
    stompGuard.commitAndUnlock();
    parentValue = 200; // parent change
    expect(stompGuard.value).toBe(200); // was accepted
  });

  it('does NOT guard against stomping deep changes, because StompGuard can\'t detect them', async () => {
    let parentObjValue = $state({value: 42});
    let objStompGuard: StompGuard<{value: number}>;
    const cleanup = $effect.root(() => {
      objStompGuard = new StompGuard(() => parentObjValue, (value) => parentObjValue = value);
    });
    objStompGuard!.value.value = 100; // deep change from child
    expect(objStompGuard!.value.value).toBe(100);
    await tick();
    parentObjValue.value = 200; // parent change
    expect(objStompGuard!.value.value).toBe(200); // stomped the deep change
    cleanup();
  });
});
