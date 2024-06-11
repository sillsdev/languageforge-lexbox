function regexEscape(s: string): string {
  return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'); // $& means the whole matched string
}

export function fixBrokenNestedGlobalStyles(shadowRoot: ShadowRoot) {
  // for some reason some occurences of :global() stick around
  // presumably this is a bug in the svelte css transformer: https://github.com/sveltejs/svelte/blob/272ffc5520dfff0cc4605ecf45147ee660c87bb0/packages/svelte/src/compiler/phases/3-transform/css/index.js
  // but I don't think this sort of super-edge-case bug is not going to get a lot of attention
  shadowRoot?.querySelectorAll('style').forEach((style) => {
    const regex = new RegExp(regexEscape(':is(.dark) :global(*)'), 'g');
    style.innerHTML = style.innerHTML.replace(regex, ':is(.dark *)');
  });
}
