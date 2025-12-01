<script module lang="ts">
  export type Token = {
    token: string;
  };
</script>

<script lang="ts">
  import type {Snippet} from 'svelte';
  import {env} from '$env/dynamic/public';
  import {Turnstile} from 'svelte-turnstile';
  import Form from './Form.svelte';
  import type {FormEnhance} from './types';

  const siteKey = env.PUBLIC_TURNSTILE_SITE_KEY;
  interface Props {
    enhance?: FormEnhance;
    turnstileToken?: string;
    children?: Snippet;
  }

  let { enhance, turnstileToken = $bindable(''), children }: Props = $props();

  function deliverToken({ detail: { token } }: CustomEvent<Token>): void {
    turnstileToken = token;
  }
</script>

<Form {enhance}>
  {@render children?.()}
</Form>

<section class="mt-8 flex justify-center md:justify-end">
  <!-- https://github.com/ghostdevv/svelte-turnstile -->
  <Turnstile {siteKey} on:turnstile-callback={deliverToken} />
</section>
