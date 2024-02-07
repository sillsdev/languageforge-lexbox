import type { ActionResult } from '@sveltejs/kit';
import { derived, get, type Readable } from 'svelte/store';
import { superForm, type FormOptions, type SuperForm } from 'sveltekit-superforms/client';
import type { SuperValidated, ZodValidation } from 'sveltekit-superforms';
import { superValidateSync } from 'sveltekit-superforms/client';
import type { AnyZodObject, z } from 'zod';
import type { ErrorMessage } from './types';
import { randomFormId } from '.';

export type LexFormState<S extends ZodValidation<AnyZodObject>> = Required<{ [field in (keyof z.infer<S>)]: {
  tainted: boolean; // has ever been touched/edited
  changed: boolean; // whether the current value is different than the last untainted value
  originalValue: z.infer<S>[field]; // last value that was considered untainted
  currentValue: z.infer<S>[field];
} }>;

export type LexFormErrors<S extends ZodValidation<AnyZodObject>> = SuperValidated<S, string>['errors'];

type LexSuperForm<S extends ZodValidation<AnyZodObject>> =
  SuperForm<S, string> & {
    formState: Readable<LexFormState<S>>
  };

type LexOnSubmit<S extends ZodValidation<AnyZodObject>> =
  (...args: Parameters<NonNullable<FormOptions<S, string>['onResult']>>) => Promise<ErrorMessage>;

//we've got to wrap this in our own version because we're not using the server side component, which this expects
export function lexSuperForm<S extends ZodValidation<AnyZodObject>>(
  schema: S,
  onSubmit: LexOnSubmit<S>,
  options: Omit<FormOptions<S, string>, 'validators'> = {},
): LexSuperForm<S> {
  const form = superValidateSync(schema, { id: options.id ?? randomFormId() });
  const sf: SuperForm<S, string> = superForm<S>(form, {
    validators: schema as any, // eslint-disable-line @typescript-eslint/no-explicit-any, @typescript-eslint/no-unsafe-assignment
    dataType: 'json',
    SPA: true, // eslint-disable-line @typescript-eslint/naming-convention
    invalidateAll: false,
    ...options,
    onResult: async (event) => {
      await options.onResult?.(event);
      const result = event.result as ActionResult<{ form: SuperValidated<S> }>;
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
  return { formState, ...sf };
}

function formHasMessageOrErrors<S extends ZodValidation<AnyZodObject>>(form: SuperForm<S, string>): boolean {
  if (get(form.message)) {
    return true;
  }

  const allErrors = get(form.allErrors);
  return !!allErrors.find(error => error.messages.find(e => e));
}

function getFormState<S extends ZodValidation<AnyZodObject>>(sf: SuperForm<S, string>): Readable<LexFormState<S>> {
  const untaintedValues = { ...get(sf.form) };
  const fieldStateStore: Readable<LexFormState<S>> = derived([sf.form, sf.tainted], ([form, tainted]) => {
    const fields = Object.keys(sf.fields) as (keyof S)[];
    const taintedFields = Object.keys(tainted ?? {}) as (keyof S)[];
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
