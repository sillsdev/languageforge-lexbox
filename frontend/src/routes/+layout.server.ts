import type { LayoutServerLoadEvent } from './$types'
import { getRootTraceparent } from '$lib/otel/server'
import { getUser } from '$lib/user'

export function load({ cookies, depends }: LayoutServerLoadEvent) {
  const user = getUser(cookies)
  if (user) {
    depends(`user:${user.id}`);
  }

  const traceParent = getRootTraceparent()

  return {
    user,
    traceParent,
  }
}
