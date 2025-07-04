//*// v1 Run with normal Svelte App (advantages: tailwind classes stay up to date)

import 'vite/modulepreload-polyfill';
import './lib/append-head-hack';
import './app.postcss';

import App from './App.svelte';
import {mount} from 'svelte';
import {setupDotnetServiceProvider} from './lib/services/service-provider-dotnet';
import {setupServiceProvider} from '$lib/services/service-provider';
import {useEventBus} from '$lib/services/event-bus';
import {setLanguage} from '$lib/i18n/LocalizationPicker.svelte';

setupServiceProvider();
setupDotnetServiceProvider();
useEventBus();

//dont mount the app until after we've loaded the local
void setLanguage('default')
  .then(() => {
    mount(App, {
      target: document.getElementById('svelte-app')!,
    });
  });
