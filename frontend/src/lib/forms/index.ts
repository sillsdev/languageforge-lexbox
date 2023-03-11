import Button from './Button.svelte'
import Form from './Form.svelte'
import ProtectedForm, { type Token } from './ProtectedForm.svelte'
import Input from './Input.svelte'

function validate_email(email: string) {
	// https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input/email#basic_validation
	return /^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$/.test(email)
}

export {
	Button,
	Form,
	ProtectedForm,
	type Token,
	Input,
	validate_email,
}
