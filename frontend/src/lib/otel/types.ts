import { isObject, isObjectWhere } from '$lib/util/types';

import type { ErrorSource } from './otel.shared';
import type { SpanContext } from '@opentelemetry/api';

export type TraceId = string;
export type TracerId = ErrorSource;
export interface Traceable {
  spanContext: SpanContext | undefined;
  tracer: TracerId;
}
export interface Traced {
  readonly spanContext: SpanContext;
  readonly tracer: TracerId;
}

export function isTraced(value: unknown): value is Traced {
  return isObjectWhere<Traced>(value, traced => traced.spanContext !== undefined);
}

export function isTraceable(value: unknown): value is Traceable {
  return isObject(value);
}

class TraceItError extends Error {
  constructor(readonly tracedValue: Traceable, message: string) {
    super(message);
  }
}

export function traceIt(traceable: Traceable, spanContext: SpanContext, tracer: TracerId): void {
  if (traceable.spanContext) {
    throw new TraceItError(traceable, `Object has already been traced (${traceable.spanContext.traceId} vs ${spanContext.traceId}).`);
  }

  if (!spanContext) {
    throw new TraceItError(traceable, `No spanContext provided.`);
  }

  traceable.spanContext = spanContext;
  traceable.tracer = tracer;

  if (traceable.spanContext != spanContext) {
    throw new TraceItError(traceable, `spanContext not writeable.`);
  }
}
