export function fixBrokenNestedGlobalStyles(shadowRoot: ShadowRoot) {
  // for some reason a second occurence of :global() sticks around on the second part of some selectors
  // e.g. the css has occurences of :is(.dark) :global(.dark\:text-yellow-200) { ... }
  // presumably this is a bug in the svelte css transformer: https://github.com/sveltejs/svelte/blob/272ffc5520dfff0cc4605ecf45147ee660c87bb0/packages/svelte/src/compiler/phases/3-transform/css/index.js
  // but I don't think this sort of super-edge-case bug is not going to get a lot of attention
  shadowRoot?.querySelectorAll('style').forEach((style) => {
    style.innerHTML = style.innerHTML.replace(/:global\(([^\(\)]+)\)/g, '$1');
  });
}
