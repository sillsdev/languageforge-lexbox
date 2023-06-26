import { type MixinConstructor, isObjectWhere } from '$lib/util/types';

export type TraceId = string;
export interface Traceable {
  readonly traceId: TraceId | undefined;
  trace: (traceId: TraceId) => void;
}
export interface Traced {
  readonly traceId: TraceId;
}

export const isTraced = (value: unknown): value is Traced => {
  return isObjectWhere<Traced>(value, traced => traced.traceId !== undefined);
}

export const isTraceable = (value: unknown): value is Traceable => {
  return isObjectWhere<Traceable>(value, traceable => typeof traceable.trace === 'function');
}

class TraceableError extends Error {
  constructor(readonly tracedValue: Traceable, message: string) {
    super(message);
  }
}

/* eslint-disable @typescript-eslint/naming-convention */
// eslint-disable-next-line @typescript-eslint/explicit-function-return-type
export function TraceableMixin<TBase extends MixinConstructor>(Base: TBase) {

  return class _Traceable extends Base implements Traceable {

    #traceId: TraceId | undefined;

    get traceId(): TraceId | undefined {
      return this.#traceId;
    }

    trace(traceId: TraceId): void {
      if (this.#traceId) {
        throw new TraceableError(this, `Object has already been traced (${this.#traceId} vs ${traceId}).`);
      }

      if (!traceId) {
        throw new TraceableError(this, `No traceId provided.`);
      }

      this.#traceId = traceId;
    }
  };
}
