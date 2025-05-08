<script lang="ts">
    type IconSize = 'text-4xl' | 'text-2xl';
    interface Props {
        size?: IconSize;
    }

    let { size = 'text-2xl' }: Props = $props();
</script>

<!-- https://icon-sets.iconify.design/healthicons/ui-user-profile/ -->
<span class="i-mdi-account-circle {size}"></span>
