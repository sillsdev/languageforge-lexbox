import {vi} from 'vitest';

export function polyfillMockAnimations() {
  // eslint-disable-next-line @typescript-eslint/unbound-method
  Element.prototype.animate ??= vi.fn().mockImplementation(() => ({
    finished: Promise.resolve(),
    cancel() {},
  }));
};
