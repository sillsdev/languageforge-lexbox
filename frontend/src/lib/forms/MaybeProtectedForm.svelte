<script lang="ts">
  import type { Snippet } from 'svelte';
  import Form from './Form.svelte';
  import ProtectedForm from './ProtectedForm.svelte';
  import type { AnySuperForm } from './types';

  interface Props {
    enhance?: AnySuperForm['enhance'];
    turnstileToken?: string;
    skipTurnstile?: boolean;
    children?: Snippet;
  }

  let { enhance, turnstileToken = $bindable(''), skipTurnstile = false, children }: Props = $props();
</script>

{#if skipTurnstile}
  <Form {enhance}>
    {@render children?.()}
  </Form>
{:else}
  <ProtectedForm {enhance} bind:turnstileToken>
    {@render children?.()}
  </ProtectedForm>
{/if}
