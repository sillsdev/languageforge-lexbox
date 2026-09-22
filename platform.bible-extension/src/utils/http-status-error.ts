/**
 * Thrown when the backend actually responded with a non-OK status, as opposed to the request
 * failing to reach it at all (e.g. connection refused while FW Lite is still starting up). Callers
 * can use this distinction to avoid treating a transient/unreachable failure the same as a
 * definitive backend response.
 */
export class HttpStatusError extends Error {
  constructor(
    readonly status: number,
    message: string,
  ) {
    super(message);
  }
}
