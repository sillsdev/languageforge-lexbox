import {vi} from 'vitest';

export function polyfillMockAnimations() {
  Element.prototype.animate = vi.fn().mockImplementation(() => ({
    finished: Promise.resolve(),
    cancel() {},
  }));
};
