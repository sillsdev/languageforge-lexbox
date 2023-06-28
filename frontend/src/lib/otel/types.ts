import { isObject, isObjectWhere } from '$lib/util/types';

export type TraceId = string;
export interface Traceable {
  traceId: TraceId | undefined;
}
export interface Traced {
  readonly traceId: TraceId;
}

export const isTraced = (value: unknown): value is Traced => {
  return isObjectWhere<Traced>(value, traced => traced.traceId !== undefined);
}

export const isTraceable = (value: unknown): value is Traceable => {
  return isObject(value);
}

class TraceItError extends Error {
  constructor(readonly tracedValue: Traceable, message: string) {
    super(message);
  }
}

export const traceIt = (traceable: Traceable, traceId: TraceId): void => {
  if (traceable.traceId) {
    throw new TraceItError(traceable, `Object has already been traced (${traceable.traceId} vs ${traceId}).`);
  }

  if (!traceId) {
    throw new TraceItError(traceable, `No traceId provided.`);
  }

  traceable.traceId = traceId;

  if (traceable.traceId != traceId) {
    throw new TraceItError(traceable, `traceId not writeable.`);
  }
}
