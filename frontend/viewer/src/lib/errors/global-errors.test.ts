import {describe, expect, it} from 'vitest';

import {processErrorIntoDetails} from './global-errors';

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

  it('does not repeat a JS Error message in both title and detail', () => {
    const error = new Error('boom');

    const {message: title, detail} = processErrorIntoDetails({message: 'Uncaught Error: boom', error});

    expect(title).toBe('boom');
    expect(detail).not.toContain('boom');
    expect(detail).toContain('at ');
  });
});
