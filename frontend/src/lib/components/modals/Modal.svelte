<script lang="ts" context="module">
	export const enum CloseReason {
		Cancel = 'cancel',
		Submit = 'submit',
	}
</script>

<script lang="ts">
	import { createEventDispatcher } from 'svelte';
	import t from '$lib/i18n';
	import { writable, type Unsubscriber } from 'svelte/store';
	const dispatch = createEventDispatcher<{
		close: CloseReason;
		open: void;
		submit: void;
	}>();

	let closeReason = writable<CloseReason | null>(null);
	let open = false;
	$: closing = $closeReason !== null && open;
	export let onBottom = false;
	export let showCloseButton = true;
	export async function openModal(autoCloseOnCanel = true, autoCloseOnSubmit = false) {
		$closeReason = null;
		open = true;
		dispatch('open');
		const result = await new Promise<CloseReason>((resolve) => {
			let unsub: Unsubscriber;
			unsub = closeReason.subscribe((reason) => {
				if (reason) {
					unsub();
					resolve(reason);
				}
			});
		});
		if (autoCloseOnCanel && result === CloseReason.Cancel) {
			close();
		}
		if (autoCloseOnSubmit && result === CloseReason.Submit) {
			close();
		}
		return result;
	}
	export function cancelModal() {
		$closeReason = CloseReason.Cancel;
	}
	export function submitModal() {
		$closeReason = CloseReason.Submit;
	}

	export function close() {
		open = false;
	}

	$: if ($closeReason === CloseReason.Submit) {
		dispatch('submit');
	}
	$: if (!open && $closeReason !== null) {
		dispatch('close', $closeReason);
	}
	let dialog: HTMLDialogElement | undefined;
	//dialog will still work if the browser doesn't support it, but this enables focus trapping and other features
	$: if (dialog) {
		if (open) {
			//showModal might be undefined if the browser doesn't support dialog
			dialog.showModal?.call(dialog);
		} else {
			dialog.close?.call(dialog);
		}
	}
</script>
<svelte:options accessors={true}/>
{#if open}
	<div class="modal" class:modal-bottom={onBottom} class:modal-open={open}>
		<dialog bind:this={dialog} class="modal-box max-w-3xl relative" class:mb-0={onBottom}>
			{#if showCloseButton}
				<button
					class="btn btn-sm btn-circle absolute right-2 top-2"
					aria-label={$t('close')}
					on:click={cancelModal}>✕</button
				>
			{/if}
			<slot />
			{#if $$slots.actions}
				<div class="modal-action">
					<slot name="actions" {closing} />
				</div>
			{/if}
		</dialog>
	</div>
{/if}
