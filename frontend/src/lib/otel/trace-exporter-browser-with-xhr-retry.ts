// We explicitly reference the browser version so that we have proper types

import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http/build/src/platform/browser';
import type { ReadableSpan } from '@opentelemetry/sdk-trace-web';

// these save us a dependency
type SendOnErrorCallback = Parameters<OTLPTraceExporter['send']>[2];
type ExporterConfig = ConstructorParameters<typeof OTLPTraceExporter>[0];

export class OTLPTraceExporterBrowserWithXhrRetry extends OTLPTraceExporter {

  private readonly xhrTraceExporter: OTLPTraceExporter;

  constructor(config?: ExporterConfig) {
    super(config);
    this.xhrTraceExporter = new OTLPTraceExporter({
      ...(config ?? {}),
      // passing a truthy value here causes XHR to be used: https://github.com/open-telemetry/opentelemetry-js/blob/main/experimental/packages/otlp-exporter-base/src/platform/browser/OTLPExporterBrowserBase.ts#L40
      headers: {},
    });
  }

  send(items: ReadableSpan[], onSuccess: () => void, onError: SendOnErrorCallback): void {
    super.send(items, onSuccess, (error) => {
      if (error.message.toLocaleLowerCase().includes('beacon')) {
        this.xhrTraceExporter.send(items, onSuccess, (xhrError) => {
          onError({
            ...error,
            message: `${error.message} --- [XHR retry message: ${xhrError.message}; code: ${xhrError.code}].`,
            code: error.code,
            data: `${error.data} --- [XHR retry data: ${xhrError.data}].`,
          });
        });
      } else {
        onError(error);
      }
    });
  }
}
