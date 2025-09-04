<script lang="ts" module>
    import type {IAuthService, IServerStatus} from '$lib/dotnet-types';
    import {type Readable, writable, type Writable} from 'svelte/store';
    import {t} from 'svelte-i18n-lingui';

    let shouldUseSystemWebViewStore: Writable<boolean> | undefined = undefined;

    function useSystemWebView(authService: IAuthService): Readable<boolean> {
        if (shouldUseSystemWebViewStore) return shouldUseSystemWebViewStore;
        shouldUseSystemWebViewStore = writable(true);
        void authService.useSystemWebView().then(r => shouldUseSystemWebViewStore!.set(r));
        return shouldUseSystemWebViewStore;
    }
</script>

<script lang="ts">
  import * as ResponsiveMenu from '$lib/components/responsive-menu';
  import type {ILexboxServer} from '$lib/dotnet-types';
  import {useAuthService} from '$lib/services/service-provider';
  import {Button} from '$lib/components/ui/button';

  const authService = useAuthService();
  const shouldUseSystemWebView = useSystemWebView(authService);

  interface Props {
    status: Omit<IServerStatus, 'displayName'>;
    text?: string | undefined;
    statusChange?: (status: 'logged-in' | 'logged-out') => void;
  }

  let {
    status,
    text = undefined,
    statusChange = () =>{    }
  }: Props = $props();
  let server = $derived(status.server);
  let loading = $state(false);


  async function login(server: ILexboxServer) {
    loading = true;
    try {
      await authService.signInWebView(server);
      statusChange('logged-in');
    } finally {
      loading = false;
    }
  }

  async function logout(server: ILexboxServer) {
    loading = true;
    try {
      await authService.logout(server);
      statusChange('logged-out');
    } finally {
      loading = false;
    }
  }
</script>

{#if status.loggedIn}
  <ResponsiveMenu.Root>
    <ResponsiveMenu.Trigger>
      {#snippet child({ props })}
        <Button {...props} {loading} icon="i-mdi-account-circle">
          {status.loggedInAs}
        </Button>
        {/snippet}
    </ResponsiveMenu.Trigger>
    <ResponsiveMenu.Content>
      <ResponsiveMenu.Item icon="i-mdi-logout" onSelect={() => logout(server)}>
        {$t`Logout`}
      </ResponsiveMenu.Item>
    </ResponsiveMenu.Content>
  </ResponsiveMenu.Root>
{:else}
    {#if $shouldUseSystemWebView}
        <Button {loading}
                variant="secondary"
                onclick={() => login(server)}
                icon="i-mdi-login">
          {text ?? $t`Login to see projects`}
        </Button>
    {:else}
        <Button {loading}
                variant="secondary"
                href="/api/auth/login/{server.id}"
                external
                icon="i-mdi-login">
          {text ?? $t`Login to see projects`}
        </Button>
    {/if}
{/if}
