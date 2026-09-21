/* eslint-disable @typescript-eslint/naming-convention */ // window.lexbox.ServiceProvider is PascalCase by design
import {afterEach, describe, expect, it, vi} from 'vitest';

import {copyText} from '$lib/services/clipboard';
import {DotnetService} from '$lib/dotnet-types';

function setClipboard(clipboard: unknown): void {
  Object.defineProperty(globalThis.navigator, 'clipboard', {value: clipboard, configurable: true});
}

describe('copyText', () => {
  afterEach(() => {
    setClipboard(undefined);
    delete (globalThis.window as {lexbox?: unknown}).lexbox;
    vi.restoreAllMocks();
  });

  it('uses the browser Clipboard API when it is available', async () => {
    const writeText = vi.fn().mockResolvedValue(undefined);
    setClipboard({writeText});
    const copyToClipboard = vi.fn().mockResolvedValue(undefined);
    (globalThis.window as {lexbox?: unknown}).lexbox = {
      ServiceProvider: {tryGetService: vi.fn().mockReturnValue({copyToClipboard})},
    };

    await copyText('hello');

    expect(writeText).toHaveBeenCalledExactlyOnceWith('hello');
    expect(copyToClipboard).not.toHaveBeenCalled();
  });

  it('falls back to the .NET bridge when navigator.clipboard is undefined (insecure Apple WebView)', async () => {
    setClipboard(undefined);
    const copyToClipboard = vi.fn().mockResolvedValue(undefined);
    const tryGetService = vi.fn().mockReturnValue({copyToClipboard});
    (globalThis.window as {lexbox?: unknown}).lexbox = {ServiceProvider: {tryGetService}};

    await copyText('hello');

    expect(tryGetService).toHaveBeenCalledWith(DotnetService.PlatformFeaturesService);
    expect(copyToClipboard).toHaveBeenCalledExactlyOnceWith('hello');
  });

  it('throws when neither the Clipboard API nor the bridge is available', async () => {
    setClipboard(undefined);
    (globalThis.window as {lexbox?: unknown}).lexbox = {
      ServiceProvider: {tryGetService: vi.fn().mockReturnValue(undefined)},
    };

    await expect(copyText('hello')).rejects.toThrow(/not available/i);
  });
});
