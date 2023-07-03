import type { ActionResult } from '@sveltejs/kit';
import { get } from 'svelte/store';
import { superForm, type FormOptions, type SuperForm } from 'sveltekit-superforms/client';
import type { SuperValidated, ZodValidation } from 'sveltekit-superforms';
import { superValidateSync } from 'sveltekit-superforms/client';
import type { AnyZodObject } from 'zod';

//we've got to wrap this in our own version because we're not using the server side component, which this expects
export function lexSuperForm<S extends ZodValidation<AnyZodObject>>(
  schema: S,
  onSubmit: NonNullable<FormOptions<S, string>['onUpdate']>,
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
      const messageBefore = get(sf.message);
      await options.onResult?.(event);
      const result = event.result as ActionResult<{ form: SuperValidated<S> }>;
      if (result.type == 'success' && result.data) {
        await onSubmit({ form: result.data.form, formEl: event.formEl, cancel: event.cancel });
        // sometimes during submit the message is set using the store that's returned from setup,
        // instead of setting it via the form passed in to the submit method. This detects that and updates the message correctly
        const messageAfter = get(sf.message);
        if (result.data.form.message === undefined && messageAfter !== messageBefore) {
          result.data.form.message = messageAfter;
        }
      }
    },
  });
  return sf;
}
