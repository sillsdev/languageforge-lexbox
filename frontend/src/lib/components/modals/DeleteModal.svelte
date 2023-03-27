<script lang="ts">
    import Modal, {CloseReason} from './Modal.svelte';
    import t from '$lib/i18n';

    export let entityName: string;
    export let isRemoveDialog = false;
    let modal: Modal;

    export async function prompt(deleteCallback?: () => Promise<void>) {
        if ((await modal.openModal()) === CloseReason.Cancel) return false;
        if (deleteCallback) await deleteCallback();
        modal.close();
        return true;
    }
</script>

<Modal bind:this={modal} showCloseButton={false}>
    <h2 class="text-xl">
        {#if isRemoveDialog}
            {$t('delete_modal.remove', {entityName})}
        {:else}
            {$t('delete_modal.delete', {entityName})}
        {/if}
    </h2>
    <slot/>
    <svelte:fragment slot="actions" let:closing>
        <button class="btn btn-error" class:loading={closing} on:click={() => modal.submitModal()}>
            <span class="i-mdi-trash text-2xl mr-2"/>
            {#if isRemoveDialog}
                {$t('delete_modal.remove', {entityName})}
            {:else}
                {$t('delete_modal.delete', {entityName})}
            {/if}
        </button>
        <button class="btn" disabled={closing} on:click={() => modal.cancelModal()}>
            {#if isRemoveDialog}
                { $t('delete_modal.dont-remove') }
            {:else}
                { $t('delete_modal.dont-delete') }
            {/if}
        </button>
    </svelte:fragment>
</Modal>
