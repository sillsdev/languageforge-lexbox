import {expect, test, beforeEach} from 'vitest'
import { get, writable, type Writable } from 'svelte/store'

import MapBindTest from './MapBind.test.svelte'
import {render} from '@testing-library/svelte'
import { tick } from 'svelte'

let inStore: Writable<string | undefined>;
let outStore: Writable<{ id: string } | undefined>;

beforeEach(() => {
  inStore = writable<string | undefined>('World');
  outStore = writable<{ id: string } | undefined>(undefined);
  render(MapBindTest, {
    a: inStore,
    b: outStore,
  });
});

test('syncs initial value from in->out', () => {
  expect(get(inStore)).toBe('World');
  expect(get(outStore)).toStrictEqual({ id: 'World' });
});


test('syncs future values from in->out and out->in', async () => {
  inStore.set('Hello');
  expect(get(inStore)).toBe('Hello');
  await tick();
  expect(get(outStore)).toStrictEqual({ id: 'Hello' });

  outStore.set({ id: 'Goodbye' });
  expect(get(outStore)).toStrictEqual({ id: 'Goodbye' });
  await tick();
  expect(get(inStore)).toBe('Goodbye');

  outStore.set(undefined);
  await tick();
  expect(get(outStore)).toBe(undefined);
  expect(get(inStore)).toBe(undefined);
});

test('in wins conflicts', async () => {
  inStore.set('Nope');
  outStore.set({ id: 'Yes' });
  await tick();
  expect(get(outStore)).toStrictEqual({ id: 'Nope' });
  expect(get(inStore)).toBe('Nope');
});
