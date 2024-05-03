//*// v1 Run with normal Svelte App (advantages: tailwind classes stay up to date)

import './app.postcss';

import App from './App.svelte';

new App({
  target: document.getElementById('app')!,
});

/*/// v2 Run with web-component in shadow dom

import './web-component';

//*/
