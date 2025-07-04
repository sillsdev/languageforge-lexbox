//*// v1 Run with normal Svelte App (advantages: tailwind classes stay up to date)

import 'vite/modulepreload-polyfill';
import './lib/append-head-hack';
import './app.postcss';
import '@formatjs/intl-durationformat/polyfill';

import App from './App.svelte';
import {mount} from 'svelte';
import {setupDotnetServiceProvider} from './lib/services/service-provider-dotnet';
import {setupServiceProvider} from '$lib/services/service-provider';
import {useEventBus} from '$lib/services/event-bus';

setupServiceProvider();
setupDotnetServiceProvider();
useEventBus();

mount(App, {
  target: document.getElementById('svelte-app')!,
});
