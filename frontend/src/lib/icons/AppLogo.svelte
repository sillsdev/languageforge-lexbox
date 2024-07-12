<script>
  import logoMono from '$lib/assets/logo-mono.svg?raw';
  let className = '';
  export {className as class};
</script>

<svg class={className} fill="currentColor">
  <!-- eslint-disable-next-line svelte/no-at-html-tags-->
  {@html logoMono}
</svg>
