import type { ActionResult } from '@sveltejs/kit';
import { get } from 'svelte/store';
import { superForm, type FormOptions, type SuperForm } from 'sveltekit-superforms/client';
import type { SuperValidated, ZodValidation } from 'sveltekit-superforms';
import { superValidateSync } from 'sveltekit-superforms/client';
import type { AnyZodObject } from 'zod';

//we've got to wrap this in our own version because we're not using the server side component, which this expects
export function lexSuperForm<S extends ZodValidation<AnyZodObject>>(
  schema: S,
  onSubmit: NonNullable<FormOptions<S, string>['onResult']>,
  options: Omit<FormOptions<S, string>, 'validators'> = {},
): SuperForm<S, string> {
  const form = superValidateSync(schema);
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
        await onSubmit(event);
        if (formHasMessageOrErrors(sf)) { // detect any messages or errors set in the onAwait callback
          event.cancel();
        }
      }
    },
  });
  return sf;
}

function formHasMessageOrErrors<S extends ZodValidation<AnyZodObject>>(form: SuperForm<S, string>): boolean {
  if (get(form.message)) {
    return true;
  }

  const allErrors = get(form.allErrors);
  return !!allErrors.find(error => error.messages.find(e => e));
}
