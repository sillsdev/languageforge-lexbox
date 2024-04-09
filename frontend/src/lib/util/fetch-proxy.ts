// Wraps fetch with the provided handler
export function handleFetch(fetchHandler: (input: {
  fetch: Fetch,
  args: Parameters<Fetch>
}) => Promise<Response>): void {
  instrumentGlobalFetch(() => { // wrapping simply abstracts away our proxy
    const currProxy = window.fetch;
    window.fetch = (...args) => fetchHandler({fetch: currProxy, args});
  });
}


// Runs instrumentation that operates on the global fetch
export function instrumentGlobalFetch(instrument: () => void): void {
  const currFetch = window.fetch;
  if (!window.lexbox.fetchProxy) throw new Error('fetchProxy not set');
  window.fetch = window.lexbox.fetchProxy; // Put our proxy in place to be instrumented
  try {
    instrument();
    window.lexbox.fetchProxy = window.fetch; // Put our (now) instrumented proxy back into place
  } finally {
    window.fetch = currFetch;
  }
}
