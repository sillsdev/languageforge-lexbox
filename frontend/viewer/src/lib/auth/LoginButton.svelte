<script context="module" lang="ts">
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
    import {mdiLogout} from '@mdi/js';
    import {Menu, MenuItem, Toggle} from 'svelte-ux';
    import type {ILexboxServer} from '$lib/dotnet-types';
    import {useAuthService} from '$lib/services/service-provider';
    import {createEventDispatcher} from 'svelte';
    import {Button} from '$lib/components/ui/button';

    const authService = useAuthService();
    const shouldUseSystemWebView = useSystemWebView(authService);
    const dispatch = createEventDispatcher<{
        status: 'logged-in' | 'logged-out'
    }>();
    export let status: IServerStatus;
    $: server = status.server;
    let loading = false;


    async function login(server: ILexboxServer) {
        loading = true;
        try {
            await authService.signInWebView(server);
            dispatch('status', 'logged-in');
        } finally {
            loading = false;
        }
    }

    async function logout(server: ILexboxServer) {
        loading = true;
        try {
            await authService.logout(server);
            dispatch('status', 'logged-out');
        } finally {
            loading = false;
        }
    }
</script>

{#if status.loggedIn}
    <Toggle let:on={open} let:toggle let:toggleOff>
      <Button onclick={toggle} {loading} icon="i-mdi-account-circle">
        {status.loggedInAs}
        <Menu {open} on:close={toggleOff} placement="bottom-end">
          <MenuItem icon={mdiLogout} on:click={() => logout(server)}>{$t`Logout`}</MenuItem>
        </Menu>
      </Button>
    </Toggle>
{:else}
    {#if $shouldUseSystemWebView}
        <Button {loading}
                variant="secondary"
                onclick={() => login(server)}
                icon="i-mdi-login">
          {$t`Login to see projects`}
        </Button>
    {:else}
        <Button {loading}
                variant="secondary"
                href="/api/auth/login/{server.id}"
                icon="i-mdi-login">
          {$t`Login to see projects`}
        </Button>
    {/if}
{/if}
