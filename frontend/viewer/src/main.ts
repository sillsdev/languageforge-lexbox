//*// v1 Run with normal Svelte App (advantages: tailwind classes stay up to date)
import 'vite/modulepreload-polyfill';
import './lib/append-head-hack';
import './app.postcss';

import App from './App.svelte';
import {setupServiceProvider} from '$lib/services/service-provider';
import {setupDotnetServiceProvider} from './lib/services/service-provider-dotnet';
import {useEventBus} from '$lib/services/event-bus';

setupServiceProvider();
setupDotnetServiceProvider();
useEventBus();
new App({
  target: document.getElementById('svelte-app')!,
});
