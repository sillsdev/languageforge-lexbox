// One dialog for the whole app, mounted in App.svelte: the toast that opens it outlives any single view.
export const whatsNew = $state({open: false});

export function openWhatsNew(): void {
  whatsNew.open = true;
}
