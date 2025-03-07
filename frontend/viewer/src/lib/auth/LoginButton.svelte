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
    import {mdiAccountCircle, mdiLogin, mdiLogout} from '@mdi/js';
    import {Button, Menu, MenuItem, Toggle} from 'svelte-ux';
    import type {ILexboxServer} from '$lib/dotnet-types';
    import {useAuthService} from '$lib/services/service-provider';
    import {createEventDispatcher} from 'svelte';

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
      <Button on:click={toggle} {loading} variant="fill" color="primary" icon={mdiAccountCircle}>
        {status.loggedInAs}
        <Menu {open} on:close={toggleOff} placement="bottom-end">
          <MenuItem icon={mdiLogout} on:click={() => logout(server)}>{$t`Logout`}</MenuItem>
        </Menu>
      </Button>
    </Toggle>
{:else}
    {#if $shouldUseSystemWebView}
        <Button {loading}
                variant="fill-light"
                color="primary"
                on:click={() => login(server)}
                icon={mdiLogin}>
          {$t`Login to see projects`}
        </Button>
    {:else}
        <Button {loading}
                variant="fill-light"
                color="primary"
                href="/api/auth/login/{server.id}"
                icon={mdiLogin}>
          {$t`Login to see projects`}
        </Button>
    {/if}
{/if}
