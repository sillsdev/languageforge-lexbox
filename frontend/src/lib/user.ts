import type {Cookies} from '@sveltejs/kit'
import {writable} from 'svelte/store'
import jwtDecode from 'jwt-decode'
import {browser} from '$app/environment'

type LexAuthUser = {
    id: string
    name: string
    email: string
    role: 'admin' | 'user'
    projects: UserProjects[]
}

type UserProjects = {
    code: string
    role: 'Manager' | 'Editor'
}

export const user = writable<LexAuthUser | null>()

export async function login(user_id: string, password: string, protection_token: string = '') {
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
        }),
    })
    if (!response.ok) {
        return false;
    }
    user.set(jwtToUser(await response.json()));
    return true;
}
type RegisterResponse = {error?: {turnstile: boolean, accountExists: boolean}, user?: LexAuthUser};
export async function register(password: string, name: string, email: string, turnstileToken: string): Promise<RegisterResponse> {
    const response = await fetch('/api/User/registerAccount', {
        method: 'post',
        headers: {
            'content-type': 'application/json',
        },
        body: JSON.stringify({
            name,
            email,
            turnstileToken,
            passwordHash: await hash(password),
        })
    });
    const responseJson = await response.json();
    if (!response.ok) {
        const error = responseJson.errors;
        return {error: {turnstile: 'TurnstileToken' in error, accountExists: 'Email' in error}};
    }
    const userJson: LexAuthUser = jwtToUser(responseJson);
    user.set(userJson);
    return {user: userJson};
}

export function get_user(cookies: Cookies): LexAuthUser | null {
    const token = cookies.get('.LexBoxAuth')

    if (!token) {
        return null
    }

    return jwtToUser(jwtDecode<any>(token));
}

function jwtToUser(user: any): LexAuthUser {
    const {sub: id, name, email, proj: projects, role} = user;

    return {
        id,
        name,
        email,
        role,
        projects,
    }
}

export function get_user_id(cookies: Cookies): string | undefined {
    return get_user(cookies)?.id
}

export function logout(cookies?: Cookies) {
    browser && clear()
    cookies && cookies.delete('.LexBoxAuth')
}

function clear() {
    user.set(null)
}

async function hash(password: string) {
    const msgUint8 = new TextEncoder().encode(password) // encode as (utf-8) Uint8Array
    const hashBuffer = await crypto.subtle.digest('SHA-1', msgUint8) // hash the message
    const hashArray = Array.from(new Uint8Array(hashBuffer)) // convert buffer to byte array
    const hashHex = hashArray.map(b => b.toString(16).padStart(2, '0')).join('') // convert bytes to hex string

    return hashHex
}

export function is_authn(cookies: Cookies) {
    return !!cookies.get('.LexBoxAuth')
}
