import Button from './Button.svelte'
import Form from './Form.svelte'
import ProtectedForm, { type Token } from './ProtectedForm.svelte'
import Input from './Input.svelte'
import Select from "./Select.svelte";
import { superForm, type EnhancedForm } from 'sveltekit-superforms/client';
import type { AnyZodObject, z } from 'zod';
import {superValidate} from "sveltekit-superforms/server";

function validate_email(email: string) {
	// https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input/email#basic_validation
	return /^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$/.test(email)
}

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
	validate_email,
	Select
}
