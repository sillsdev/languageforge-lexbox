import {afterEach, beforeEach, describe, expect, it} from 'vitest';
import {findFirstTabbable, findNextTabbable} from './tabbable';

describe('tabbable utilities', () => {
  let container: HTMLDivElement;

  beforeEach(() => {
    // Create a fresh container for each test
    container = document.createElement('div');
    document.body.appendChild(container);
  });

  afterEach(() => {
    // Clean up after each test
    document.body.removeChild(container);
  });

  describe('findFirstTabbable', () => {
    it('should return undefined for null container', () => {
      expect(findFirstTabbable(null)).toBeUndefined();
    });

    it('should return undefined for undefined container', () => {
      expect(findFirstTabbable(undefined)).toBeUndefined();
    });

    it('should return null when no tabbable elements exist', () => {
      container.innerHTML = '<div>No tabbable elements</div>';
      expect(findFirstTabbable(container)).toBeNull();
    });

    it('should find the first tabbable element', () => {
      container.innerHTML = `
        <div>
          <span>Not tabbable</span>
          <button id="first">First button</button>
          <button id="second">Second button</button>
          <button id="third">Third button</button>
        </div>
      `;
      const result = findFirstTabbable(container);
      expect(result?.id).toBe('first');
    });

    it('should find tabbable element with explicit tabindex', () => {
      container.innerHTML = `
        <div>
          <span tabindex="0" id="tabbable-span">Tabbable span</span>
          <button id="button">Button</button>
        </div>
      `;
      const result = findFirstTabbable(container);
      expect(result?.id).toBe('tabbable-span');
    });
  });

  describe('findNextTabbable', () => {
    it('should return undefined when current is null', () => {
      expect(findNextTabbable(null)).toBeUndefined();
    });

    it('should return undefined when current is undefined', () => {
      expect(findNextTabbable(undefined)).toBeUndefined();
    });

    it('should return current element when it has no parent', () => {
      const isolated = document.createElement('button');
      expect(findNextTabbable(isolated)).toBe(isolated);
    });

    it('should throw error when current element is not tabbable', () => {
      container.innerHTML = '<div id="not-tabbable">Not tabbable</div>';
      const notTabbable = container.querySelector('#not-tabbable') as HTMLElement;

      expect(() => findNextTabbable(notTabbable)).toThrow(
        'Current element is not tabbable, so can\'t find relative tabbable element'
      );
    });

    it('should find the next tabbable element in same container', () => {
      container.innerHTML = `
        <div>
          <button id="first">First</button>
          <button id="second">Second</button>
          <button id="third">Third</button>
        </div>
      `;
      const first = container.querySelector('#first') as HTMLElement;
      const result = findNextTabbable(first);
      expect(result?.id).toBe('second');
    });

    it('should wrap to first element when at the end', () => {
      container.innerHTML = `
        <div>
          <button id="first">First</button>
          <button id="second">Second</button>
          <button id="third">Third</button>
        </div>
      `;
      const third = container.querySelector('#third') as HTMLElement;
      const result = findNextTabbable(third);
      expect(result?.id).toBe('first');
    });

    it('should traverse up the DOM when no next element in current container', () => {
      container.innerHTML = `
        <div>
          <button id="before">Before</button>
          <div>
            <button id="current">Current</button>
          </div>
          <button id="after">After</button>
        </div>
      `;
      const current = container.querySelector('#current') as HTMLElement;
      const result = findNextTabbable(current);
      expect(result?.id).toBe('after');
    });

    it('should handle nested containers correctly', () => {
      container.innerHTML = `
        <div>
          <button id="first">First</button>
          <div>
            <div>
              <button id="nested">Nested</button>
            </div>
          </div>
          <button id="last">Last</button>
        </div>
      `;
      const nested = container.querySelector('#nested') as HTMLElement;
      const result = findNextTabbable(nested);
      expect(result?.id).toBe('last');
    });

    it('should work with mixed tabbable elements', () => {
      container.innerHTML = `
        <div>
          <button id="button">Button</button>
          <input id="input" type="text" />
          <a id="link" href="#">Link</a>
          <select id="select"><option>Option</option></select>
          <textarea id="textarea"></textarea>
        </div>
      `;
      const button = container.querySelector('#button') as HTMLElement;
      const input = findNextTabbable(button);
      expect(input?.id).toBe('input');
      const link = findNextTabbable(input);
      expect(link?.id).toBe('link');
      const select = findNextTabbable(link);
      expect(select?.id).toBe('select');
      const textarea = findNextTabbable(select);
      expect(textarea?.id).toBe('textarea');
      const buttonAgain = findNextTabbable(textarea);
      expect(buttonAgain?.id).toBe('button'); // wraps back to first element
    });

    it('should handle elements with custom tabindex', () => {
      container.innerHTML = `
        <div>
          <button id="button" tabindex="1">Button</button>
          <div id="div" tabindex="0">Div</div>
          <span id="span" tabindex="2">Span</span>
        </div>
      `;
      // Note: tabbable library handles tabindex ordering
      const button = container.querySelector('#button') as HTMLElement;
      const span = findNextTabbable(button);
      expect(span?.id).toBe('span');
    });

    it('should handle single tabbable element by wrapping to itself', () => {
      container.innerHTML = `
        <div>
          <button id="only">Only button</button>
        </div>
      `;
      const only = container.querySelector('#only') as HTMLElement;
      const result = findNextTabbable(only);
      expect(result?.id).toBe('only');
    });

    it('should work when starting from deeply nested element', () => {
      container.innerHTML = `
        <div>
          <button id="first">First</button>
          <div>
            <div>
              <div>
                <div>
                  <button id="deep">Deep</button>
                </div>
              </div>
            </div>
          </div>
          <button id="last">Last</button>
        </div>
      `;
      const deep = container.querySelector('#deep') as HTMLElement;
      const result = findNextTabbable(deep);
      expect(result?.id).toBe('last');
    });
  });
});
