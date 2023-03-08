import { writable } from 'svelte/store'

export const user = writable<User>()

type User = {
	id: number,
	name: string,
	email: string,
	role: 'admin' | 'user',
	projects: Project[],
}

type Project = {
	id: string,
	name: string,
}

