/// <reference types='@sveltejs/kit' />
namespace App {
    interface Locals {
        user: any;
        client: import('@urql/svelte').Client;
    }
}