import {
  HOST_MESSAGE_SOURCE,
  PLUGIN_API_VERSION,
  PluginApiException,
  isPluginMessage,
  type HostCapabilities,
  type HostInitMessage,
  type HostResponseMessage,
  type PluginPermission,
} from './plugin-api-types';
import type {PluginApiAdapter} from './plugin-api-adapter';

export interface PluginHostConfig {
  projectName: string;
  projectCode: string;
  permissions: PluginPermission[];
  capabilities: HostCapabilities;
  entryId?: string;
}

/** A plugin this saturated is broken or hostile; refusing keeps the app responsive. */
const MAX_IN_FLIGHT_REQUESTS = 100;

/**
 * The app side of the plugin postMessage bridge: answers exactly one iframe, verified by
 * window identity. Wire it to `<svelte:window onmessage>` via {@link handleWindowMessage}.
 */
export class PluginHost {
  #iframe: HTMLIFrameElement | undefined;
  #inFlight = 0;

  constructor(
    private adapter: PluginApiAdapter,
    private config: PluginHostConfig,
  ) {}

  attach(iframe: HTMLIFrameElement): void {
    this.#iframe = iframe;
  }

  detach(): void {
    this.#iframe = undefined;
  }

  readonly handleWindowMessage = (event: MessageEvent): void => {
    const contentWindow = this.#iframe?.contentWindow;
    if (!contentWindow || event.source !== contentWindow) return;
    if (!isPluginMessage(event.data)) return;
    const message = event.data;
    if (message.kind === 'sdk-ready') {
      this.#post({
        source: HOST_MESSAGE_SOURCE,
        v: 1,
        kind: 'init',
        apiVersion: PLUGIN_API_VERSION,
        project: {projectName: this.config.projectName, projectCode: this.config.projectCode},
        theme: document.documentElement.classList.contains('dark') ? 'dark' : 'light',
        permissions: this.config.permissions,
        capabilities: this.config.capabilities,
        ...(this.config.entryId ? {context: {entryId: this.config.entryId}} : {}),
      });
      return;
    }
    if (message.kind === 'request') {
      if (this.#inFlight >= MAX_IN_FLIGHT_REQUESTS) {
        this.#postError(message.id, new PluginApiException('internal', 'Too many concurrent requests'));
        return;
      }
      void this.#handleRequest(message.id, message.method, message.args ?? []);
    }
  };

  async #handleRequest(id: number, method: string, args: unknown[]): Promise<void> {
    this.#inFlight++;
    // RPC boundary: failures belong to the calling plugin, not the app's global error handler.
    try {
      const result = await this.adapter.handle(method, args);
      this.#post({source: HOST_MESSAGE_SOURCE, v: 1, kind: 'response', id, ok: true, result});
    } catch (error) {
      this.#postError(id, error);
    } finally {
      this.#inFlight--;
    }
  }

  #postError(id: number, error: unknown): void {
    const isApiError = error instanceof PluginApiException;
    this.#post({
      source: HOST_MESSAGE_SOURCE,
      v: 1,
      kind: 'response',
      id,
      ok: false,
      error: {
        code: isApiError ? error.code : 'internal',
        message: error instanceof Error ? error.message : String(error),
      },
    });
  }

  #post(message: HostInitMessage | HostResponseMessage): void {
    // The sandboxed iframe has an opaque origin, so '*' is the only usable target origin;
    // confidentiality comes from posting directly to the plugin's contentWindow.
    this.#iframe?.contentWindow?.postMessage(message, '*');
  }
}
