import type { LayoutServerLoadEvent } from './$types'
import { getRootTraceparent } from '$lib/otel/server'
import { getUser } from '$lib/user'

export function load({ cookies }: LayoutServerLoadEvent) {
  const user = getUser(cookies)
  const traceParent = getRootTraceparent()

  return {
    user,
    traceParent,
  }
}
