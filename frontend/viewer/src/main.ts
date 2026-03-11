//*// v1 Run with normal Svelte App (advantages: tailwind classes stay up to date)

import 'vite/modulepreload-polyfill';
import './lib/append-head-hack';
import './app.css';
import '@formatjs/intl-durationformat/polyfill';

import App from './App.svelte';
import {mount} from 'svelte';
import {setupDotnetServiceProvider} from './lib/services/service-provider-dotnet';
import {setupServiceProvider} from '$lib/services/service-provider';
import {setupBrowserAppServices} from '$lib/services/browser-app-services';
import {useEventBus} from '$lib/services/event-bus';
import {setLanguage} from '$lib/i18n';

setupServiceProvider();
setupDotnetServiceProvider();
if (!window.lexbox.isDotnetHosted) {
  setupBrowserAppServices();
}
useEventBus();

//don't mount the app until after we've loaded the local
void setLanguage('default')
  .then(() => {
    mount(App, {
      target: document.getElementById('svelte-app')!,
    });
  });
