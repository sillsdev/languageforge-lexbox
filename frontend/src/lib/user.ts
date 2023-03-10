import { browser } from '$app/environment'
import type { Cookies } from '@sveltejs/kit'
import { writable } from 'svelte/store'

export const user = init()

function init() {
	const { set, subscribe, update } = writable()

	return {
		set: new_val => {
			if (new_val) {
				sessionStorage.setItem('user', JSON.stringify(new_val))

			} else {
				sessionStorage.removeItem('user')
			}

			set(new_val)
		},
		subscribe,
		update,
	}
}

if (browser) {
	user.set(JSON.parse(sessionStorage.getItem('user')))
}

export async function login(user_id: string, password: string) {
	clear()

	const response = await fetch('/api/login', {
		method: 'post',
		headers: {
			'content-type': 'application/json',
		},
		body: JSON.stringify({
			emailOrUsername: user_id,
			password: await hash(password),
			preHashedPassword: true,
		})
	})

	if (response.ok) {
		const {
			sub: id,
			name,
			email,
			role,
			proj: projects
		} = await response.json()

		user.set({
			id,
			name: name || email.split('@')[0],
			email,
			role,
			projects,
		})

		return true
	}

	return false
}

export function get_authn_token(cookies: Cookies) {
	return cookies.get('.LexBoxAuth')
}

export function logout(cookies: Cookies) {
	cookies.delete('.LexBoxAuth')
}

export function clear() {
	user.set(null)
}

async function hash(password: string) {
	const msgUint8 = new TextEncoder().encode(password) // encode as (utf-8) Uint8Array
	const hashBuffer = await crypto.subtle.digest('SHA-1', msgUint8) // hash the message
	const hashArray = Array.from(new Uint8Array(hashBuffer)) // convert buffer to byte array
	const hashHex = hashArray.map(b => b.toString(16).padStart(2, '0')).join('') // convert bytes to hex string

	return hashHex
}
