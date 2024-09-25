<script lang="ts">
  import { TitlePage } from '$lib/layout';
  import t from '$lib/i18n';
  import { goto } from '$app/navigation';
  import { acceptInvitation } from '$lib/user';

  import type { Token } from '$lib/forms/ProtectedForm.svelte';
  import { env } from '$env/dynamic/public';
  import { Turnstile } from 'svelte-turnstile';
  import { onMount } from 'svelte';
  import { getSearchParamValues } from '$lib/util/query-params';

  type AcceptInvitationImmediatelyPageQueryParams = {
    name: string;
    email: string;
  };

  const siteKey = env.PUBLIC_TURNSTILE_SITE_KEY;
  let resolveToken: (token: string) => void;
  let turnstileToken = new Promise<string>((resolve, _) => { resolveToken = resolve });

  function deliverToken({ detail: { token } }: CustomEvent<Token>): void {
    resolveToken(token);
  }

  onMount(async () => {
    const urlValues = getSearchParamValues<AcceptInvitationImmediatelyPageQueryParams>();
    let token = await turnstileToken;
    const registerResponse = await acceptInvitation('x', 0, urlValues.name ?? 'x', urlValues.email, 'x', token);
    console.log(registerResponse);
    if (!registerResponse.error) await goto('/home', { invalidateAll: true }); // invalidate so we get the user from the server
    //TODO: Display errors on page, if any
  });
</script>

<section class="mt-8 flex justify-center">
  <Turnstile {siteKey} on:turnstile-callback={deliverToken} />
</section>
