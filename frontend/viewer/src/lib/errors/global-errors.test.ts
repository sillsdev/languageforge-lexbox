import {describe, expect, it} from 'vitest';

import {processErrorIntoDetails, unifyErrorEvent} from './global-errors';

describe('processErrorIntoDetails', () => {
  it('splits a .NET error at the first stack frame', () => {
    const message = [
      'System.InvalidOperationException: Everything is broken.',
      '   at FwLiteShared.Services.Foo.Bar(String x)',
      '   at FwLiteShared.Services.Foo.Baz()',
    ].join('\n');

    const {message: title, detail} = processErrorIntoDetails({message, error: null});

    expect(title).toBe('System.InvalidOperationException: Everything is broken.');
    expect(detail).toContain('at FwLiteShared.Services.Foo.Bar(String x)');
  });

  it('splits at the inner-exception marker that precedes the outer frames, keeping a wrapped error\'s title short', () => {
    // MSAL wrapping an Android network failure: the whole " ---> " cascade comes before the first managed frame
    const message = [
      'Microsoft.Identity.Client.MsalServiceException: Failed to retrieve OIDC configuration. See inner exception.',
      ' ---> System.Net.Http.HttpRequestException: Connection failure',
      ' ---> Java.Net.UnknownHostException: Unable to resolve host',
      '   at Java.Interop.JniEnvironment.InstanceMethods.CallVoidMethod()',
    ].join('\n');

    const {message: title, detail} = processErrorIntoDetails({message, error: null});

    expect(title).toBe('Microsoft.Identity.Client.MsalServiceException: Failed to retrieve OIDC configuration. See inner exception.');
    expect(detail).toContain('---> System.Net.Http.HttpRequestException: Connection failure');
  });

  it('keeps the whole message as the title when there is no .NET stack', () => {
    const {message: title, detail} = processErrorIntoDetails({message: 'plain error', error: null});

    expect(title).toBe('plain error');
    expect(detail).toBeUndefined();
  });

  it('shows frames-only detail for a JS error, not repeating the title', () => {
    const error = new Error('boom');

    const {message: title, detail} = processErrorIntoDetails(unifyErrorEvent(error));

    expect(title).toBe('Error: boom');
    expect(detail).not.toContain('boom');
    expect(detail).toContain('at ');
  });
});

describe('unifyErrorEvent', () => {
  it('uses Error.toString() (type kept, no "Uncaught" prefix) for an ErrorEvent with an error', () => {
    const error = new TypeError('x is not a function');
    const event = {message: 'Uncaught TypeError: x is not a function', error, filename: 'f', lineno: 1, colno: 2};

    const unified = unifyErrorEvent(event as unknown as ErrorEvent);

    expect(unified.message).toBe('TypeError: x is not a function');
    expect(unified.error).toBe(error);
  });

  it('uses Error.toString() for a rejected Error reason', () => {
    const error = new TypeError('x is not a function');

    const unified = unifyErrorEvent({reason: error} as unknown as PromiseRejectionEvent);

    expect(unified.message).toBe('TypeError: x is not a function');
    expect(unified.error).toBe(error);
  });

  it('falls back to the event message for an ErrorEvent without an error object', () => {
    const event = {message: 'Script error.', error: null, filename: '', lineno: 0, colno: 0};

    const unified = unifyErrorEvent(event as unknown as ErrorEvent);

    expect(unified.message).toBe('Script error.');
    expect(unified.error).toBeNull();
  });
});
