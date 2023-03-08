import { fail, redirect } from '@sveltejs/kit'
import type { Actions } from './$types'

export const actions = {
	default: async ({ cookies, fetch, request}) => {
		const user_inputs = await request.formData()

		const user_id = user_inputs.get('user_id') as string
		if (! user_id) {
			return fail<ErrorResponse>(400, { user_id, error: { user_id: 'User info missing' } })
		}

		const password = user_inputs.get('password') as string
		if (! password) {
			return fail<ErrorResponse>(400, { user_id, error: { password: 'Password missing' } })
		}

// 		const response = await fetch('/api/login')
// console.log({response})

		const user = await stub_login(user_id, password).catch(() => {
			return fail<ErrorResponse>(400, { user_id, error: { bad_credentials: true } })
		})

		cookies.set('user', JSON.stringify(user))

		throw redirect(302, '/')
	}
} satisfies Actions

async function stub_login(user_id: string, password: string) {
	console.log(`simulating credential check for ${user_id} : ${password}`)

	return new Promise((resolve, reject) => {
		if (password === 'abc123') {
			resolve({
				id: 1,
				name: 'Big Poppa',
				email: 'bigpoppa@example.org',
				role: 'user',
				projects: [
					{
						id: 'project-1',
						name: 'project-1',
					},
				],
			})
		} else {
			reject()
		}
	})
}

type ErrorResponse = {
	user_id: string,
	error: {
		user_id?: string,
		password?: string,
		bad_credentials?: boolean,
	}
}
