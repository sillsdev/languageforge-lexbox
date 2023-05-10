import Button from './Button.svelte'
import Form from './Form.svelte'
import ProtectedForm, { type Token } from './ProtectedForm.svelte'
import Input from './Input.svelte'
import Select from "./Select.svelte";
import { superForm, type SuperForm, type FormOptions } from 'sveltekit-superforms/client';
import type { AnyZodObject } from 'zod';
import {superValidate} from "sveltekit-superforms/server";
import type { Validation, ZodValidation } from 'sveltekit-superforms/index';
import type { ActionResult } from '@sveltejs/kit';

//we've got to wrap this in our own version because we're not using the server side component, which this expects
export function lexSuperForm<S extends ZodValidation<AnyZodObject>>(schema: S, onSubmit: NonNullable<FormOptions<S, string>["onUpdate"]>, options: Omit<FormOptions<S, string>, 'validators'> = {}): SuperForm<S, string> {
	return superForm<S>(undefined, {
		validators: schema as any,
		dataType: 'json',
		SPA: true,
		invalidateAll: false,
		 ...options,
		 onResult: async (event) => {
			await options.onResult?.(event);
			const result = event.result as ActionResult<{form: Validation<S>}>;
			if (result.type == "success" && result.data) {
				await onSubmit({form: result.data.form, cancel: event.cancel});
			}
		 },
	});
}

//again not using the server side component, so we have to wrap this
export async function lexSuperValidate<S extends AnyZodObject>(form: SuperForm<S>["form"], schema: S): Promise<void> {
	await superValidate(form, schema);
}

export {
	Button,
	Form,
	ProtectedForm,
	type Token,
	Input,
	Select
}
