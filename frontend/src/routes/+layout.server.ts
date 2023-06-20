import type { LayoutServerLoadEvent } from './$types'
import { getRootTraceparent } from '$lib/otel/server'

export function load({ locals }: LayoutServerLoadEvent) {
  const user = locals.getUser();
  const traceParent = getRootTraceparent()

  return {
    user,
    traceParent,
  }
}
