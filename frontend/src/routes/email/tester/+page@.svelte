<script lang="ts">
  import { onMount } from 'svelte';
  import { componentMap } from '../emails';

  export const templates = Object.keys(componentMap);
  let currTemplate = templates[0];

  let iframe: HTMLIFrameElement;

  function resizeIframe(): void {
    iframe.height = `${(iframe.contentWindow?.document.body.scrollHeight ?? 700)}px`;
  }

  onMount(resizeIframe);
</script>

<label class="label cursor-pointer inline-flex gap-4 m-4">
  <span class="label-text">Email template:</span>
  <select class="select select-info" bind:value={currTemplate}>
    {#each templates as template}
      <option value={template}>{template.replaceAll(/([a-z])([A-Z])/g, '$1 $2')}</option>
    {/each}
  </select>
</label>

<div class="m-4 mockup-window border bg-white">
  <iframe
    bind:this={iframe}
    width="100%"
    src="./tester/raw?type={currTemplate}"
    title={currTemplate}
    on:load={resizeIframe}
  />
</div>
