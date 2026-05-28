import { expect, test } from '@playwright/test';
import { LoginPage } from './pages/loginPage';
import { defaultPassword } from './envVars';
import { AdminDashboardPage } from './pages/adminDashboardPage';

test('can navigate to project page', async ({ page }) => {
  const loginPage = await new LoginPage(page).goto();
  await loginPage.fillForm('admin', defaultPassword);
  await loginPage.submit();
  const adminPage = await new AdminDashboardPage(page).waitFor();
  await adminPage.openProject('Sena 3', 'sena-3');
});

// Regression test for #2224. With the buggy $derived binding, keystrokes that arrive while
// the debounced write is round-tripping through the URL store get clobbered when the derived
// re-runs. Small per-keystroke delays (≤ ~100ms) reliably interleave with that round-trip;
// slow typing does not reproduce because the round-trip completes between keystrokes.
test.describe('user filter typing', () => {
  test.use({ ignoreHTTPSErrors: true });

  test('does not lose characters while typing rapidly', async ({ page }) => {
    const loginPage = await new LoginPage(page).goto();
    await loginPage.fillForm('admin', defaultPassword);
    await loginPage.submit();
    const adminPage = await new AdminDashboardPage(page).waitFor();

    const input = adminPage.userFilterBarInput;
    await input.click();

    // Drive keystrokes via dispatched input events so the timing is precise and not throttled
    // by the CDP keyboard pipeline. Mixed small delays (1–100ms) interleave with the debounce
    // round-trip the way real typing does.
    const text = 'abcdefghij';
    const delays = [1, 1, 5, 5, 10, 10, 40, 40, 100, 100];
    await input.evaluate(async (el: HTMLInputElement, { text, delays }) => {
      const setter = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value')!.set!;
      el.focus();
      for (let i = 0; i < text.length; i++) {
        setter.call(el, el.value + text[i]);
        el.dispatchEvent(new InputEvent('input', { bubbles: true, data: text[i], inputType: 'insertText' }));
        if (i < text.length - 1) await new Promise(r => setTimeout(r, delays[i]));
      }
    }, { text, delays });

    // Wait for any debounce + URL round-trip to settle.
    await page.waitForTimeout(800);

    const actual = await input.inputValue();
    // The bug surfaces as some-but-not-all characters surviving; an empty input would mean the
    // keystrokes never landed at all (a setup failure, not the bug we're guarding against).
    expect.soft(actual, 'input must not be empty after typing').not.toBe('');
    expect(actual, 'input must contain every character typed').toBe(text);
  });
});
