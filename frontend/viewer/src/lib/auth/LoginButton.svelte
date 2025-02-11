<script context="module" lang="ts">
    import type {IAuthService, IServerStatus} from '$lib/dotnet-types';
    import {type Readable, writable, type Writable} from 'svelte/store';

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
    import {isNoSuchHostError} from '$lib/errors/global-errors';

    const authService = useAuthService();
    const shouldUseSystemWebView = useSystemWebView(authService);
    const dispatch = createEventDispatcher<{
        status: 'logged-in' | 'logged-out'
    }>();
    export let status: IServerStatus;
    $: server = status.server;
    let loading = false;
    let loginError: string | undefined;

    async function login(server: ILexboxServer) {
        loading = true;
        loginError = undefined;
        try {
            await authService.signInWebView(server);
            dispatch('status', 'logged-in');
        } catch (e) {
            console.error(e);
            if (isNoSuchHostError(e)) {
                loginError = `Failed to connect. Are you online?`;
            } else {
                throw e;
            }
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
          <MenuItem icon={mdiLogout} on:click={() => logout(server)}>Logout</MenuItem>
        </Menu>
      </Button>
    </Toggle>
{:else}
  <div class="flex flex-col gap-2">
    {#if $shouldUseSystemWebView}
        <Button {loading}
                variant="fill-light"
                color="primary"
                on:click={() => login(server)}
                icon={mdiLogin}>
            Login to see projects
        </Button>
    {:else}
        <Button {loading}
                variant="fill-light"
                color="primary"
                href="/api/auth/login/{server.id}"
                icon={mdiLogin}>
              Login to see projects
        </Button>
    {/if}
    {#if loginError}
        <span class="text-warning text-sm">{loginError}</span>
    {/if}
  </div>
{/if}
