<script lang="ts">
  import type { Snippet } from 'svelte';
  import Form from './Form.svelte';
  import ProtectedForm from './ProtectedForm.svelte';
  import type { AnySuperForm } from './types';

  interface Props {
    enhance?: AnySuperForm['enhance'] | undefined;
    turnstileToken?: string;
    skipTurnstile?: boolean;
    children?: Snippet;
  }

  let {
    enhance = undefined,
    turnstileToken = $bindable(''),
    skipTurnstile = false,
    children,
    ...rest
  }: Props = $props();
</script>

{#if skipTurnstile}
  <!-- TODO: This used to have an on:submit attribute to bubble up the submit event from HTML.
       Let's check if the createBubbler() call in Form makes that unnecessary now. -->
  <Form {enhance} {...rest}>
    {@render children?.()}
  </Form>
{:else}
  <!-- TODO: This used to have an on:submit attribute to bubble up the submit event from HTML.
       Let's check if the createBubbler() call in Form makes that unnecessary now. -->
  <ProtectedForm {enhance} bind:turnstileToken {...rest}>
    {@render children?.()}
  </ProtectedForm>
{/if}
