<script context="module" lang="ts">
    import type {IAuthService} from '$lib/dotnet-types';
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
    import {mdiLogin, mdiLogout} from '@mdi/js';
    import {Button} from 'svelte-ux';
    import type {ILexboxServer} from '$lib/dotnet-types';
    import {useAuthService} from '$lib/services/service-provider';
    import {createEventDispatcher} from 'svelte';

    const authService = useAuthService();
    const shouldUseSystemWebView = useSystemWebView(authService);
    const dispatch = createEventDispatcher<{
        status: 'logged-in' | 'logged-out'
    }>();
    export let isLoggedIn: boolean;
    export let server: ILexboxServer;
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

{#if isLoggedIn}
    <Button {loading}
            variant="fill"
            color="primary"
            on:click={() => logout(server)}
            icon={mdiLogout}>
        Logout
    </Button>
{:else}
    {#if $shouldUseSystemWebView}
        <Button {loading}
                variant="fill-light"
                color="primary"
                on:click={() => login(server)}
                icon={mdiLogin}>
            Login
        </Button>
    {:else}
        <Button {loading}
                variant="fill-light"
                color="primary"
                href="/api/auth/login/{server.id}"
                icon={mdiLogin}>
            Login
        </Button>
    {/if}
{/if}
