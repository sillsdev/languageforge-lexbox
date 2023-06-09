import ForgotPassword from '$lib/email/ForgotPassword.svelte';
import { render } from '$lib/email/emailRenderer.server';

export function load(): { subject: string, html: string } {
  return render(ForgotPassword, { name: 'John Doe' });
}
