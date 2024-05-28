<script lang="ts">
  import Form from './Form.svelte';
  import ProtectedForm from './ProtectedForm.svelte';
  import type { AnySuperForm } from './types';

  export let enhance: AnySuperForm['enhance'] | undefined = undefined;
  export let turnstileToken = '';
  export let skipTurnstile = false;
</script>

{#if skipTurnstile}
<Form {enhance} on:submit>
  <slot />
</Form>
{:else}
<ProtectedForm {enhance} on:submit bind:turnstileToken>
  <slot />
</ProtectedForm>
{/if}
