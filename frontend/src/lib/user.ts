import { browser } from '$app/environment'
import { redirect, type Cookies } from '@sveltejs/kit'
import jwtDecode from 'jwt-decode'
import { removeAllNotifications } from './notify'

type RegisterResponseErrors = {
  errors: {
    /* eslint-disable @typescript-eslint/naming-convention */
    TurnstileToken?: unknown,
    Email?: unknown,
    /* eslint-enable @typescript-eslint/naming-convention */
  }
}

type JwtTokenUser = {
  sub: string
  name: string
  email: string
  role: 'admin' | 'user'
  proj: UserProjects[],
  unver: boolean | undefined,
}

export type LexAuthUser = {
  id: string
  name: string
  email: string
  role: 'admin' | 'user'
  projects: UserProjects[]
  emailVerified: boolean
}

type UserProjects = {
  code: string
  role: 'Manager' | 'Editor'
}

export const isAdmin = (user: LexAuthUser | null): boolean => user?.role === 'admin';

export async function login(userId: string, password: string): Promise<boolean> {
  const response = await fetch('/api/login', {
    method: 'post',
    headers: {
      'content-type': 'application/json',
    },
    body: JSON.stringify({
      emailOrUsername: userId,
      password: hash(password),
      preHashedPassword: true,
    }),
  })
  return response.ok;
}

type RegisterResponse = { error?: { turnstile: boolean, accountExists: boolean }, user?: LexAuthUser };
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

  if (!response.ok) {
    const { errors } = await response.json() as RegisterResponseErrors;
    if (!errors) throw new Error('Missing error on non-ok response');
    return { error: { turnstile: 'TurnstileToken' in errors, accountExists: 'Email' in errors } };
  }

  const responseJson = await response.json() as JwtTokenUser;
  const userJson: LexAuthUser = jwtToUser(responseJson);
  return { user: userJson };
}

export function getUser(cookies: Cookies): LexAuthUser | null {
  const token = cookies.get('.LexBoxAuth')

  if (!token) {
    return null
  }

  return jwtToUser(jwtDecode<JwtTokenUser>(token));
}

function jwtToUser(user: JwtTokenUser): LexAuthUser {
  const { sub: id, name, email, proj: projects, role } = user;

  return {
    id,
    name,
    email,
    role,
    projects,
    emailVerified: !user.unver,
  }
}

export function logout(cookies?: Cookies): void {
  cookies && cookies.delete('.LexBoxAuth')
  removeAllNotifications();
  if (browser && window.location.pathname !== '/login') {
    throw redirect(307, '/login');
  }
}

export async function hash(password: string): Promise<string> {
  const msgUint8 = new TextEncoder().encode(password) // encode as (utf-8) Uint8Array
  let hashBuffer: ArrayBuffer;
  const c = typeof crypto !== 'undefined' ? crypto : await import('node:crypto');
  if (c && c.subtle) {
    hashBuffer = await c.subtle.digest('SHA-1', msgUint8) // hash the message
  } else {
    console.log('crypto.subtle not found; cryptop module was', c);
    throw new Error('crypto.subtle not found -- are we running on an old version of Node?');
  }
  const hashArray = Array.from(new Uint8Array(hashBuffer)) // convert buffer to byte array
  const hashHex = hashArray.map(b => b.toString(16).padStart(2, '0')).join('') // convert bytes to hex string

  return hashHex
}

export function isAuthn(cookies: Cookies): boolean {
  return !!cookies.get('.LexBoxAuth')
}

export async function refreshJwt(): Promise<void> {
  await fetch('/api/login/refresh', { method: 'POST' });
}
