import Button from './Button.svelte'
import Form from './Form.svelte'
import ProtectedForm, { type Token } from './ProtectedForm.svelte'
import Input from './Input.svelte'
import Select from "./Select.svelte";
import { superForm, type SuperForm, type FormOptions } from 'sveltekit-superforms/client';
import type { AnyZodObject, z } from 'zod';
import {superValidate} from "sveltekit-superforms/server";

//we've got to wrap this in our own version because we're not using the server side component, which this expects
export function lexSuperForm<S extends AnyZodObject>(schema: S, options: Omit<FormOptions<S, string>, 'validators'> = {}): SuperForm<S, string> {
	return superForm(undefined, {
		validators: schema,
		dataType: 'json',
		SPA: true,
		 ...options});
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
