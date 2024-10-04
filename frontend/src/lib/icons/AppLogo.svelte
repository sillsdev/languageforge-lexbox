<script>
  import logoDark from '$lib/assets/logo-dark.svg';
  import logoLight from '$lib/assets/logo-light.svg';
  import logoMono from '$lib/assets/logo-mono.svg?raw';
  let className = '';
  export {className as class};

  export let mono = false;
</script>

{#if mono}
<svg class={className} fill="currentColor">
  <!-- eslint-disable-next-line svelte/no-at-html-tags-->
  {@html logoMono}
</svg>
{:else}
<picture class="my-0 {className}">
  <source srcset={logoLight} media="(prefers-color-scheme: dark)">
  <img src={logoDark} alt="Lexbox logo">
</picture>
{/if}
