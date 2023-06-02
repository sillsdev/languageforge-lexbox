/// <reference types='@sveltejs/kit' />

export {}; // for some reason this is required in order to make global changes

declare global {
	  interface Window {
		    fetch_original: typeof window.fetch;
		    fetch_otel_instrumented: typeof window.fetch;
    }

    namespace App {
        interface Locals {
            user: any;
            client: import('@urql/svelte').Client;
        }
    }
}
