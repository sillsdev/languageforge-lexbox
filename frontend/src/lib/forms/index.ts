import Button from './Button.svelte'
import Form, { type Token } from './Form.svelte'
import Input from './Input.svelte'

function validate_email(email: string) {
	// https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input/email#basic_validation
	return /^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$/.test(email)
}

export {
	Button,
	Form,
	type Token,
	Input,
	validate_email,
}
