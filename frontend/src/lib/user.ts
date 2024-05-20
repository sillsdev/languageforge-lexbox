import { browser } from '$app/environment'
import { redirect, type Cookies } from '@sveltejs/kit'
import { jwtDecode } from 'jwt-decode'
import { deleteCookie, getCookie } from './util/cookies'
import {hash} from '$lib/util/hash';
import { ensureErrorIsTraced, errorSourceTag } from './otel'
import zxcvbn from 'zxcvbn';
import { type AuthUserProject, ProjectRole, UserRole, type CreateGuestUserByAdminInput } from './gql/types';
import { _createGuestUserByAdmin } from '../routes/(authenticated)/admin/+page';

type LoginError = 'BadCredentials' | 'Locked';
type LoginResult = {
  error?: LoginError,
  success: false,
} | { success: true };

type RegisterResponseErrors = {
  errors: {
    /* eslint-disable @typescript-eslint/naming-convention */
    TurnstileToken?: unknown,
    Email?: unknown,
    Required?: unknown,
    /* eslint-enable @typescript-eslint/naming-convention */
  }
}

type JwtTokenUser = {
  sub: string
  name: string
  email?: string
  user?: string
  role: 'admin' | 'user'
  proj?: string,
  lock: boolean | undefined,
  unver: boolean | undefined,
  mkproj: boolean | undefined,
  creat: boolean | undefined,
  loc: string,
}

export type LexAuthUser = {
  id: string
  name: string
  email?: string
  username?: string
  emailOrUsername: string
  role: UserRole
  isAdmin: boolean
  projects: AuthUserProject[]
  locked: boolean
  emailVerified: boolean
  canCreateProjects: boolean
  createdByAdmin: boolean
  locale: string
}

export const USER_LOAD_KEY = 'user:current';
export const AUTH_COOKIE_NAME = '.LexBoxAuth';

export const usernameRe = /^[a-zA-Z0-9_]+$/;

export function getHomePath(user: LexAuthUser | null): string {
  return user?.isAdmin ? '/admin' : '/';
}

export async function login(userId: string, password: string): Promise<LoginResult> {
  const strength = zxcvbn(password);
  const response = await fetch('/api/login', {
    method: 'post',
    headers: {
      'content-type': 'application/json',
    },
    body: JSON.stringify({
      emailOrUsername: userId,
      password: await hash(password),
      preHashedPassword: true,
      passwordStrength: strength.score
    }),
    lexboxResponseHandlingConfig: {
      disableRedirectOnAuthError: true,
    },
  })
  return response.ok
    ? { success: true }
    : { success: false, error: await response.text() as LoginError };
}

type RegisterResponse = { error?: { turnstile: boolean, accountExists: boolean, invalidInput: boolean }, user?: LexAuthUser };
export async function createUser(endpoint: string, password: string, passwordStrength: number, name: string, email: string, locale: string, turnstileToken: string): Promise<RegisterResponse> {
  const response = await fetch(endpoint, {
    method: 'post',
    headers: {
      'content-type': 'application/json',
    },
    body: JSON.stringify({
      name,
      email,
      locale,
      turnstileToken,
      passwordStrength,
      passwordHash: await hash(password),
    })
  });

  if (!response.ok) {
    const { errors } = await response.json() as RegisterResponseErrors;
    if (!errors) throw new Error('Missing error on non-ok response');
    return { error: { turnstile: 'TurnstileToken' in errors, accountExists: 'Email' in errors, invalidInput: 'Required' in errors } };
  }

  const responseJson = await response.json() as JwtTokenUser;
  const userJson: LexAuthUser = jwtToUser(responseJson);
  return { user: userJson };
}
export function register(password: string, passwordStrength: number, name: string, email: string, locale: string, turnstileToken: string): Promise<RegisterResponse> {
  return createUser('/api/User/registerAccount', password, passwordStrength, name, email, locale, turnstileToken);
}
export function acceptInvitation(password: string, passwordStrength: number, name: string, email: string, locale: string, turnstileToken: string): Promise<RegisterResponse> {
  return createUser('/api/User/acceptInvitation', password, passwordStrength, name, email, locale, turnstileToken);
}
export async function createGuestUserByAdmin(password: string, passwordStrength: number, name: string, email: string, locale: string, _turnstileToken: string): Promise<RegisterResponse> {
  const passwordHash = await hash(password);
  const gqlInput: CreateGuestUserByAdminInput = {
    passwordHash,
    passwordStrength,
    name,
    locale,
  };
  if (email.includes('@')) {
    gqlInput.email = email;
  } else {
    gqlInput.username = email;
  }
  const gqlResponse = await _createGuestUserByAdmin(gqlInput);
  // const registerResponse = { error?: { turnstile: boolean, accountExists: boolean }, user?: LexAuthUser };
  if (gqlResponse.error?.byType('UniqueValueError')) {
    return { error: { invalidInput: false, turnstile: false, accountExists: true }};
  }
  if (gqlResponse.error?.byType('RequiredError')) {
    return { error: { invalidInput: true, turnstile: false, accountExists: false }};
  }
  if (!gqlResponse.data?.createGuestUserByAdmin.lexAuthUser ) {
    return { error: { invalidInput: true, turnstile: false, accountExists: false }};
  }
  const responseUser = gqlResponse.data?.createGuestUserByAdmin.lexAuthUser;
  const user: LexAuthUser = {
    ...responseUser,
    email: responseUser.email ?? undefined,
    username: responseUser.username ?? undefined,
    locked: responseUser.locked ?? false,
    emailVerified: responseUser.emailVerificationRequired ?? false,
    canCreateProjects: responseUser.canCreateProjects ?? false,
    createdByAdmin: responseUser.createdByAdmin ?? false,
    emailOrUsername: (responseUser.email ?? responseUser.username) as string,
  }
  return { user }
}

export function getUser(cookies: Cookies): LexAuthUser | null {
  const token = getCookie(AUTH_COOKIE_NAME, cookies);

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
  const { sub: id, name, email, user: username, proj: projectsString, role: jwtRole } = user;
  const role = Object.values(UserRole).find(r => r.toLowerCase() === jwtRole) ?? UserRole.User;

  return {
    id,
    name,
    email,
    username,
    role,
    isAdmin: role === UserRole.Admin,
    projects: projectsStringToProjects(projectsString),
    locked: user.lock === true,
    emailVerified: !user.unver,
    canCreateProjects: user.mkproj === true || role === UserRole.Admin,
    createdByAdmin: user.creat ?? false,
    locale: user.loc,
    emailOrUsername: (email ?? username) as string,
  }
}

function projectsStringToProjects(projectsString: string | undefined): AuthUserProject[] {
  if (!projectsString) return [];
  const projects: AuthUserProject[] = [];
  for (const pString of projectsString.split(',')) {
    const roleCode = pString[0];
    let role = ProjectRole.Unknown;
    switch (roleCode) {
      case 'm':
        role = ProjectRole.Manager;
        break;
      case 'e':
        role = ProjectRole.Editor;
        break;
    }
    projects.push(...pString.split('|').map(id => ({projectId: id, role})));
  }
  return projects;
}

export function logout(cookies?: Cookies): void {
  cookies && deleteCookie(AUTH_COOKIE_NAME, cookies);
  if (browser && window.location.pathname !== '/login') {
    redirect(307, '/login');
  }
}

export function isAuthn(cookies: Cookies): boolean {
  return getUser(cookies) !== null;
}

export async function refreshJwt(): Promise<void> {
  await fetch('/api/login/refresh', { method: 'POST' });
}
