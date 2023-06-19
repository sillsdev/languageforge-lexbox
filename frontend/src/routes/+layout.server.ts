import type { LayoutServerLoadEvent } from './$types'
import { getRootTraceparent } from '$lib/otel/server'
import { getUser } from '$lib/user'

export function load({ locals }: LayoutServerLoadEvent) {
  const user = locals.getUser();
  const traceParent = getRootTraceparent()

  return {
    user,
    traceParent,
  }
}
