import Button from './Button.svelte'
import Form from './Form.svelte'
import ProtectedForm, { type Token } from './ProtectedForm.svelte'
import Input from './Input.svelte'
import Select from "./Select.svelte";
import { superForm, type EnhancedForm } from 'sveltekit-superforms/client';
import type { AnyZodObject, z } from 'zod';
import {superValidate} from "sveltekit-superforms/server";

//we've got to wrap this in our own version because we're not using the server side component, which this expects
export function lexSuperForm<S extends AnyZodObject>(schema: S): EnhancedForm<S, string> {
	return superForm(undefined, {validators: schema});
}

//again not using the server side component, so we have to wrap this
export async function lexSuperValidate<S extends AnyZodObject>(form: z.infer<S>, schema: S, update: EnhancedForm<S>["update"]): Promise<void> {
	const result = await superValidate(form, schema);
	update({data: {form: result}, status: result.valid ? 200 : 400, type: result.valid ? 'success' : 'failure'});
}

export {
	Button,
	Form,
	ProtectedForm,
	type Token,
	Input,
	Select
}
