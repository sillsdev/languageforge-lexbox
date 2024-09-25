<script lang="ts">
  import t from '$lib/i18n';
  import { goto } from '$app/navigation';
  import { acceptInvitation, type RegisterResponse } from '$lib/user';

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

  let registerResponse: RegisterResponse;

  function errorText(response: RegisterResponse): string {
    const { error } = response;
    if (error) {
      if (error.turnstile) {
        return $t('turnstile.invalid');
      }
      if (error.accountExists) {
        return $t('register.account_exists_email');
      }
      return $t('register.unknown_error');
    }
    return '';
  }

  onMount(async () => {
    const urlValues = getSearchParamValues<AcceptInvitationImmediatelyPageQueryParams>();
    let token = await turnstileToken;
    registerResponse = await acceptInvitation('x', 0, urlValues.name ?? 'x', urlValues.email, 'x', token);
    if (!registerResponse.error) await goto('/home', { invalidateAll: true }); // invalidate so we get the user from the server
  });
</script>

<section class="mt-8 flex justify-center">
  {#if !(registerResponse?.error)}
    <Turnstile {siteKey} on:turnstile-callback={deliverToken} />
  {:else}
    <p>
      {errorText(registerResponse)}
    </p>
  {/if}
</section>
