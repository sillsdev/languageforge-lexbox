import type {ActionResult} from '@sveltejs/kit';
import {derived, get, type Readable} from 'svelte/store';
import {superForm, type FormOptions, type SuperForm} from 'sveltekit-superforms/client';
import type {Infer, InferIn, SuperValidated} from 'sveltekit-superforms';
import {type ZodValidation, zod} from 'sveltekit-superforms/adapters';
import {defaults} from 'sveltekit-superforms/client';
import type {ErrorMessage} from './types';
import {randomFormId} from './utils';

type FieldKeys<T extends Record<string, unknown>> = Extract<keyof T, string>;

export type LexFormState<S extends Record<string, unknown>> = Required<{ [field in FieldKeys<S>]: {
  tainted: boolean; // has ever been touched/edited
  changed: boolean; // whether the current value is different than the last untainted value
  originalValue: S[field]; // last value that was considered untainted
  currentValue: S[field];
} }>;

export type LexFormErrors<S extends Record<string, unknown>> = SuperValidated<S, string>['errors'];

type LexSuperForm<S extends ZodValidation> =
  SuperForm<Infer<S, 'zod'>, string> & {
    formState: Readable<LexFormState<Infer<S, 'zod'>>>
  };

type LexOnSubmit<S extends Record<string, unknown>> =
  (...args: Parameters<NonNullable<FormOptions<S, string>['onResult']>>) => Promise<ErrorMessage>;

//we've got to wrap this in our own version because we're not using the server side component, which this expects
export function lexSuperForm<S extends ZodValidation>(
  schema: S,
  onSubmit: LexOnSubmit<Infer<S, 'zod'>>,
  options: Omit<FormOptions<Infer<S, 'zod'>, string, InferIn<S, 'zod'>>, 'validators'> = {},
): LexSuperForm<S> {

  const adapter = zod(schema);
  const form = defaults(adapter, {
    id: options.id ?? randomFormId(),
  });
  const sf = superForm<Infer<S, 'zod'>, string, InferIn<S, 'zod'>>(form, {
    validators: adapter,
    dataType: 'json',
    SPA: true, // eslint-disable-line @typescript-eslint/naming-convention
    invalidateAll: false,
    ...options,
    onResult: async (event) => {
      await options.onResult?.(event);
      const result = event.result as ActionResult<{ form: SuperValidated<Infer<S, 'zod'>, string, InferIn<S, 'zod'>> }>;
      if (result.type == 'success' && result.data) {
        const error = await onSubmit(event);
        if (error) {
          sf.message.set(error);
        }
        if (formHasMessageOrErrors(sf)) { // detect any messages or errors set in the onSubmit callback
          event.cancel();
        }
      }
    },
  });

  const formState = getFormState(sf);
  return { formState, ...sf } as LexSuperForm<S>;
}

function formHasMessageOrErrors<S extends Record<string, unknown>>(form: SuperForm<S, string>): boolean {
  if (get(form.message)) {
    return true;
  }

  const allErrors = get(form.allErrors);
  return !!allErrors.find(error => error.messages.find(e => e));
}

function getFormState<S extends Record<string, unknown>>(sf: SuperForm<S, string>): Readable<LexFormState<S>> {
  const untaintedValues = { ...get(sf.form) };
  const fieldStateStore: Readable<LexFormState<S>> = derived([sf.form, sf.tainted], ([form, tainted]) => {
    const capture = sf.capture();
    const fields = Object.keys(capture.shape ?? capture.data) as FieldKeys<S>[];
    const taintedFields = Object.keys(tainted ?? {}) as FieldKeys<S>[];
    const untaintedFields = fields.filter(field => !taintedFields.includes(field));
    for (const untaintedField of untaintedFields) {
      untaintedValues[untaintedField] = form[untaintedField];
    }

    return fields.reduce<LexFormState<S>>((result, field) => {
      result[field] = {
        tainted: taintedFields.includes(field),
        changed: form[field] !== untaintedValues[field],
        originalValue: untaintedValues[field],
        currentValue: form[field],
      };
      return result;
    }, {} as LexFormState<S>);
  });
  return fieldStateStore;
}
