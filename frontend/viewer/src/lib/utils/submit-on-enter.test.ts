import {describe, expect, it, vi} from 'vitest';
import {isSubmitEnter} from './submit-on-enter';

const mobile = vi.hoisted(() => ({value: false}));
// eslint-disable-next-line @typescript-eslint/naming-convention
vi.mock('$lib/hooks/is-mobile.svelte', () => ({IsMobile: mobile}));

function keydown(target: HTMLElement, init: KeyboardEventInit & {keyCode?: number} = {}): KeyboardEvent {
  const event = new KeyboardEvent('keydown', {key: 'Enter', bubbles: true, ...init});
  if (init.keyCode !== undefined) Object.defineProperty(event, 'keyCode', {value: init.keyCode});
  Object.defineProperty(event, 'target', {value: target});
  return event;
}

// jsdom doesn't implement isContentEditable
function element(tag: string, contentEditable = false): HTMLElement {
  const el = document.createElement(tag);
  Object.defineProperty(el, 'isContentEditable', {value: contentEditable});
  return el;
}

describe('isSubmitEnter', () => {
  it('submits on Enter in an input or rich-text field', () => {
    expect(isSubmitEnter(keydown(element('input')))).toBe(true);
    expect(isSubmitEnter(keydown(element('div', true)))).toBe(true);
  });

  it('leaves Enter on buttons and picker triggers alone', () => {
    expect(isSubmitEnter(keydown(element('button')))).toBe(false);
  });

  it('ignores Shift+Enter, IME composition and other keys', () => {
    const input = element('input');
    expect(isSubmitEnter(keydown(input, {shiftKey: true}))).toBe(false);
    expect(isSubmitEnter(keydown(input, {isComposing: true}))).toBe(false);
    expect(isSubmitEnter(keydown(input, {keyCode: 229}))).toBe(false);
    expect(isSubmitEnter(keydown(input, {key: ' '}))).toBe(false);
  });

  it('never submits on mobile', () => {
    mobile.value = true;
    try {
      expect(isSubmitEnter(keydown(element('input')))).toBe(false);
    } finally {
      mobile.value = false;
    }
  });
});
