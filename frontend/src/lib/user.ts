import { browser } from '$app/environment'
import { redirect, type Cookies } from '@sveltejs/kit'
import { jwtDecode } from 'jwt-decode'
import { deleteCookie, getCookie } from './util/cookies'
import {hash} from '$lib/util/hash';
import { ensureErrorIsTraced, errorSourceTag } from './otel'
import { type LexboxAudience, type LexAuthUser as ApiLexAuthUser, UserRole, type AuthUserProject, ProjectRole } from './gql/types';

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
    /* eslint-enable @typescript-eslint/naming-convention */
  }
}

type JwtTokenUser = {
  sub: string
  name: string
  email: string
  role: UserRole
  proj?: string,
  lock: boolean | undefined,
  unver: boolean | undefined,
  mkproj: boolean | undefined,
  loc: string,
  aud: LexboxAudience,
}

export type LexAuthUser = Omit<Required<ApiLexAuthUser>, 'emailVerificationRequired' | 'projectsJson' | 'updatedDate'> & {
  emailVerified: boolean
}
export const USER_LOAD_KEY = 'user:current';
export const AUTH_COOKIE_NAME = '.LexBoxAuth';

export function getHomePath(user: LexAuthUser | null): string {
  return user?.isAdmin ? '/admin' : '/';
}

export async function login(userId: string, password: string): Promise<LoginResult> {
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
    lexboxResponseHandlingConfig: {
      disableRedirectOnAuthError: true,
    },
  })
  return response.ok
    ? { success: true }
    : { success: false, error: await response.text() as LoginError };
}

type RegisterResponse = { error?: { turnstile: boolean, accountExists: boolean }, user?: LexAuthUser };
export async function register(password: string, name: string, email: string, locale: string, turnstileToken: string): Promise<RegisterResponse> {
  const response = await fetch('/api/User/registerAccount', {
    method: 'post',
    headers: {
      'content-type': 'application/json',
    },
    body: JSON.stringify({
      name,
      email,
      locale,
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
  const { sub: id, name, email, proj: projectsString, role } = user;

  return {
    id,
    name,
    email,
    role,
    isAdmin: role === UserRole.Admin,
    projects: projectsStringToProjects(projectsString),
    locked: user.lock === true,
    emailVerified: !user.unver,
    canCreateProjects: user.mkproj === true,
    locale: user.loc,
    audience: user.aud,
  }
}

function projectsStringToProjects(projectsString: string | undefined): AuthUserProject[] {
  if (!projectsString) return [];
  const projects: AuthUserProject[] = [];
  for (const pString of projectsString.split(',')) {
    const roleCode = pString[0];
    let role: ProjectRole = ProjectRole.Unknown;
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
