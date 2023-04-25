<script lang="ts">
    import type {PageData} from "./$types";

    export let data: PageData;
</script>
<div class="m-3 mockup-window border bg-base-300">
    <span>Subject: {data.subject}</span>
    {@html data.html}
</div>
