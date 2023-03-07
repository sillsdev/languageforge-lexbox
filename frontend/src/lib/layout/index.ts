import { drawerStore } from '@skeletonlabs/skeleton'
import AppBar from './AppBar.svelte'
import AppMenu from './AppMenu.svelte'
import Page from './Page.svelte'

function open_menu() {
	drawerStore.open()
}

export {
	AppBar,
	AppMenu,
	open_menu,
	Page,
}
