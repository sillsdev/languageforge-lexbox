import { browser } from '$app/environment'
import { redirect, type Cookies } from '@sveltejs/kit'
import jwtDecode from 'jwt-decode'
import { deleteCookie, getCookie } from './util/cookies'
import {hash} from '$lib/util/hash';
import { ensureErrorIsTraced, errorSourceTag } from './otel'

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
  proj?: JwtTokenProject[],
  unver: boolean | undefined,
  mkproj: boolean | undefined,
}

type JwtTokenProject = {
  // eslint-disable-next-line @typescript-eslint/naming-convention
  Code: string,
  // eslint-disable-next-line @typescript-eslint/naming-convention
  ProjectId: string,
  // eslint-disable-next-line @typescript-eslint/naming-convention
  Role: 'Manager' | 'Editor',
}

export type LexAuthUser = {
  id: string
  name: string
  email: string
  role: 'admin' | 'user'
  projects: UserProjects[]
  emailVerified: boolean
  canCreateProject: boolean
}

type UserProjects = {
  projectId: string
  code: string
  role: 'Manager' | 'Editor'
}
export const USER_LOAD_KEY = 'user:current';

export function isAdmin(user: LexAuthUser | null): boolean {
  return user?.role === 'admin';
}

export function getHomePath(user: LexAuthUser | null): string {
  return isAdmin(user) ? '/admin' : '/';
}

export async function login(userId: string, password: string): Promise<boolean> {
  const response = await fetch('/api/login', {
    method: 'post',
    headers: {
      'content-type': 'application/json',
    },
    body: JSON.stringify({
      emailOrUsername: userId,
      password: await hash(password),
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
  const token = getCookie('.LexBoxAuth', cookies);

  if (!token) {
    return null
  }

  try {
    return jwtToUser(jwtDecode<JwtTokenUser>(token));
  } catch (error) {
    const traceId = ensureErrorIsTraced(error, undefined, {
      'app.error.source': errorSourceTag('jwt-decode'),
    });
    console.error(error, `Trace ID: ${traceId}.`);
    return null;
  }
}

function jwtToUser(user: JwtTokenUser): LexAuthUser {
  const { sub: id, name, email, proj: projects, role } = user;

  return {
    id,
    name,
    email,
    role,
    projects: projects?.map(p => ({code: p.Code, role: p.Role, projectId: p.ProjectId})) ?? [],
    emailVerified: !user.unver,
    canCreateProject: user.mkproj === true,
  }
}

export function logout(cookies?: Cookies): void {
  cookies && deleteCookie('.LexBoxAuth', cookies);
  if (browser && window.location.pathname !== '/login') {
    throw redirect(307, '/login');
  }
}



export function isAuthn(cookies: Cookies): boolean {
  return !!cookies.get('.LexBoxAuth')
}

export async function refreshJwt(): Promise<void> {
  await fetch('/api/login/refresh', { method: 'POST' });
}
