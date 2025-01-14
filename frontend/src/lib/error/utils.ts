export function tryGetErrorMessage(error: unknown): string | undefined {
  if (error === null || error === undefined) {
    return undefined;
  } else if (typeof error === 'string') {
    return error;
  }

  const _error = (error ?? {}) as Record<string, string>;
  return (
      _error.message ??
      _error.reason ??
      _error.cause ??
      _error.error ??
      _error.code
  );
}
