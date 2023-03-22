<script lang="ts">
    export let code: string;
    function injectStyles(arg: Event|HTMLIFrameElement) {
        const iframe = 'target' in arg ? arg.target as HTMLIFrameElement : arg;
        iframe.contentDocument?.head.insertAdjacentHTML('beforeend', `
            <style>
                .hgk-clone-btn {
                    display: none;
                }
            </style>
        `);
    }
</script>

<iframe use:injectStyles on:load={injectStyles} src="/api/hg-view/{code}" title="HG Web view" width="100%" height="600px"></iframe>
