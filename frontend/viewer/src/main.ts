//*// v1 Run with normal Svelte App (advantages: tailwind classes stay up to date)
import 'vite/modulepreload-polyfill';
import './lib/append-head-hack';
import './app.postcss';

import App from './App.svelte';
import {setupDotnetServiceProvider} from './lib/services/service-provider-dotnet';

setupDotnetServiceProvider();
new App({
  target: document.getElementById('svelte-app')!,
});
