import {render} from "$lib/email/emailRenderer.server";
import ForgotPassword from "$lib/email/ForgotPassword.svelte";

export function load() {
    return render(ForgotPassword, {name: 'John Doe'});
}