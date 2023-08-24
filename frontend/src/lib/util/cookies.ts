import type { Cookies } from '@sveltejs/kit';

// Utility functions for reading and deleting cookies that might be chunked
// https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.cookies.chunkingcookiemanager?view=aspnetcore-7.0

export function getCookie(name: string, cookies: Cookies): string | undefined {
  const cookie = cookies.get(name);

  if (!cookie) {
    return undefined;
  }

  const chunkMatch = cookie.match(/chunks-(\d)/);
  if (!chunkMatch) {
    return cookie;
  }

  const chunkCount = Number.parseInt(chunkMatch[1]);
  let chunkedCookie = '';
  for (let i = 1; i <= chunkCount; i++) {
    chunkedCookie += cookies.get(`${name}C${i}`);
  }
  return chunkedCookie;
}

export function deleteCookie(name: string, cookies: Cookies): void {
  const cookie = cookies.get(name);
  cookies.delete(name);

  if (!cookie) {
    return undefined;
  }

  const chunkMatch = cookie.match(/chunks-(\d)/);
  if (!chunkMatch) {
    return;
  }

  const chunkCount = Number.parseInt(chunkMatch[1]);
  for (let i = 1; i <= chunkCount; i++) {
    cookies.delete(`${name}C${i}`);
  }
}
