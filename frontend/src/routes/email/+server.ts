import type {RequestEvent} from './$types';
import {json} from "@sveltejs/kit";
import ForgotPassword from "$lib/email/ForgotPassword.svelte";
import {render} from "$lib/email/emailRenderer";


export function GET(request: RequestEvent) {
    return json(render(ForgotPassword, {name: 'John Doe'}));
}