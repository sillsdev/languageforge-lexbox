import {apiVersion, APP_VERSION} from '$lib/util/verstion';
import type { LayoutServerLoadEvent } from './$types'
import { getRootTraceparent } from '$lib/otel/server'

export async function load({locals, depends, fetch}: LayoutServerLoadEvent) {
  const user = locals.getUser();
  const traceParent = getRootTraceparent()

  if (user) depends(`user:${user.id}`);
  if (apiVersion.value === null) {
    const response = await fetch('/api/healtz');
    apiVersion.value = response.headers.get('lexbox-version');
  }
  return {
    user,
    traceParent,
    serverVersion: APP_VERSION,
    apiVersion: apiVersion.value
  }
}
