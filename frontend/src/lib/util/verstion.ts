export const APP_VERSION: string = import.meta.env.VITE_APP_VERSION as string | undefined ?? 'dev';
//we set the api version based on the most recent lexbox-version header from a request.
export const apiVersion = {value: null} as {value: string | null};
