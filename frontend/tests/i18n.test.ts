import { BasePage } from './pages/basePage';
import { LoginPage } from './pages/loginPage';
import { expect } from '@playwright/test';
import { test } from './fixtures';

test('server-side locale does not leak', async ({ contextFactory }) => {
  // Get 2 contexts with different languages
  var spanishContext = await contextFactory({ locale: 'es' });
  var spanishPage = await spanishContext.newPage();
  var englishContext = await contextFactory({ locale: 'en' });
  var englishPage = await englishContext.newPage();

  // Load a spanish page that pauses between server initialization and rendering
  // we need an ssr-only page so that csr doesn't magically fix the language (which is maybe a nice fallback, but doesn't work for emails)
  var spanishTask = spanishPage.goto('/sandbox/i18n/ssr-only?delay=5000');
  // make sure we've hit the delay i.e. i18n has been initialized server-side for the spanish user/request
  await spanishPage.waitForTimeout(1000);

  // Load a page in a different language
  var englishLoginPage = await new LoginPage(englishPage).goto();
  await englishLoginPage.page.getByRole('heading', { name: 'Log in' }).waitFor();

  // finishing loading the page in the initial language
  await spanishTask;
  // verify that it used the correct language
  await expect(spanishPage.locator('.current')).not.toContainText('Log in');
  await expect(spanishPage.locator('.current')).toContainText('Iniciar sesión');
});

test('late subscription to locale works', async ({ contextFactory }) => {
  // Because we've put the user's locale into the svelte context and call getContext somewhat implicitly
  // one could think that Svelte would throw an error, because we call getContext too late.
  // However, it seems to work fine and this test demonstrates that. See the page itself for more details.

  // Get a context with a locale other than our fallback (en)
  var frenchContext = await contextFactory({ locale: 'fr' });
  var frenchPage = await frenchContext.newPage();

  // Load a page that only has 1 translation that is performed after a delay (this is a slightly flawed test,
  // because there are other places in the +layout.svelte that get translated,
  // but that's true for all of our pages, so if it works here it'll work everywhere)
  await frenchPage.goto('/sandbox/i18n/late');
  await BasePage.waitForHydration(frenchPage);

  // Verify that the translation get's performed correctly
  await expect(frenchPage.locator('.delayed-current')).toContainText('Se connecter');
});
