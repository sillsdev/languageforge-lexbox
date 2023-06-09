<script context="module" lang="ts">
  export type Token = {
    token: string;
  };
</script>

<script lang="ts">
  import { env } from '$env/dynamic/public';
  import { Turnstile } from 'svelte-turnstile';
  import Form from './Form.svelte';
  import type { AnySuperForm } from './types';

  export let enhance: AnySuperForm['enhance'] | undefined = undefined;

  const siteKey = env.PUBLIC_TURNSTILE_SITE_KEY;
  export let turnstileToken = '';

  function deliverToken({ detail: { token } }: CustomEvent<Token>): void {
    turnstileToken = token;
  }
</script>

<Form {enhance} on:submit>
  <slot />
</Form>

<section class="mt-8 flex justify-center md:justify-end">
  <!-- https://github.com/ghostdevv/svelte-turnstile -->
  <Turnstile {siteKey} on:turnstile-callback={deliverToken} />
</section>
