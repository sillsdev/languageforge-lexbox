<script lang="ts">
    import type {PageData} from "./$types";

    export let data: PageData;
</script>
<div class="m-3 mockup-window border bg-white">
    <span class="text-black">Subject: {data.subject}</span>
    {@html data.html}
</div>
