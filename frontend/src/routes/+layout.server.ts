import type { LayoutServerLoadEvent } from './$types'
import { getRootTraceparent } from '$lib/otel/server'

export function load({ locals, depends }: LayoutServerLoadEvent) {
  const user = locals.getUser();
  const traceParent = getRootTraceparent()

  if (user) depends(`user:${user.id}`);

  return {
    user,
    traceParent,
  }
}
