import {describe, it, expect} from 'vitest';
import {movedIds} from './moved-ids';

describe('movedIds', () => {
  it('finds nothing when the order is unchanged', () => {
    expect([...movedIds(['a', 'b', 'c'], ['a', 'b', 'c'])]).toEqual([]);
  });

  it('marks the swapped items', () => {
    expect([...movedIds(['a', 'b', 'c'], ['a', 'c', 'b'])]).toEqual(['c', 'b']);
  });

  it('ignores shifts caused by additions and removals', () => {
    expect([...movedIds(['a', 'b', 'c'], ['x', 'a', 'c'])]).toEqual([]);
  });
});
