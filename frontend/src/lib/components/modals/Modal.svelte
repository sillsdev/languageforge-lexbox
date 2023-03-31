<script lang="ts" context="module">
	export const enum DialogResponse {
		Cancel = 'cancel',
		Submit = 'submit',
	}
</script>

<script lang="ts">
	import { createEventDispatcher } from 'svelte';
	import t from '$lib/i18n';
	import { writable, type Unsubscriber } from 'svelte/store';
	const dispatch = createEventDispatcher<{
		close: DialogResponse;
		open: void;
		submit: void;
	}>();

	let dialogResponse = writable<DialogResponse | null>(null);
	let open = false;
	$: closing = $dialogResponse !== null && open;
	export let bottom = false;
	export let showCloseButton = true;
	export async function openModal(autoCloseOnCancel = true, autoCloseOnSubmit = false) {
		$dialogResponse = null;
		open = true;
		dispatch('open');
		const response = await new Promise<DialogResponse>((resolve) => {
			let unsub: Unsubscriber;
			unsub = dialogResponse.subscribe((reason) => {
				if (reason) {
					unsub();
					resolve(reason);
				}
			});
		});
		if (autoCloseOnCancel && response === DialogResponse.Cancel) {
			close();
		}
		if (autoCloseOnSubmit && response === DialogResponse.Submit) {
			close();
		}
		return response;
	}
	export function cancelModal() {
		$dialogResponse = DialogResponse.Cancel;
	}
	export function submitModal() {
		$dialogResponse = DialogResponse.Submit;
	}

	export function close() {
		open = false;
	}

	$: if ($dialogResponse === DialogResponse.Submit) {
		dispatch('submit');
	}
	$: if (!open && $dialogResponse !== null) {
		dispatch('close', $dialogResponse);
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
{#if open}
<!-- using DaisyUI modal https://daisyui.com/components/modal/ -->
	<div class="modal" class:modal-bottom={bottom} class:modal-open={open}>
		<dialog bind:this={dialog} class="modal-box max-w-3xl relative" class:mb-0={bottom}>
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
