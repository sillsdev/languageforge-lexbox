const sayWuuuuuuut = "We're not sure what happened.";

export const getErrorMessage = (error: unknown): string => {
	if (error === null || error === undefined) {
		return sayWuuuuuuut;
	} else if (typeof error === 'string') {
		return error as string;
	}

	const _error = (error ?? {}) as Record<string, string>;
	return (
		_error.message ??
		_error.reason ??
		_error.cause ??
		_error.error ??
		_error.code ??
		sayWuuuuuuut
	);
};
