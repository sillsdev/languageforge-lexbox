import {gt} from 'svelte-i18n-lingui';

export const s = {
  get clear() {
    return gt({
      message: 'clear',
      comment: 'Button to clear/empty a text field e.g. when filtering for semantic domains',
    });
  },
};
