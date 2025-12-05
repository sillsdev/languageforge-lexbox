// We explicitly reference the browser version so that we have proper types

import {ExportResultCode, type ExportResult} from '@opentelemetry/core';
import {OTLPTraceExporter} from '@opentelemetry/exporter-trace-otlp-http/build/src/platform/browser';
import type {ReadableSpan} from '@opentelemetry/sdk-trace-web';

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

  export(items: ReadableSpan[], onResult: (result: ExportResult) => void): void {
    super.export(items, (result) => {
      if (result.code === ExportResultCode.SUCCESS) {
        onResult(result);
        return;
      }

      const error = result.error ?? new Error('OTLPTraceExporterBrowserWithXhrRetry: Unknown export error');
      if (error.message.toLocaleLowerCase().includes('beacon')) {
        // retry with XHR
        this.xhrTraceExporter.export(items, (result) => {
          if (result.code === ExportResultCode.SUCCESS) {
            onResult(result);
            return;
          }

          const xhrError = result.error ?? new Error('OTLPTraceExporterBrowserWithXhrRetry: Unknown XHR export error');
          const augmentedError = new Error(`${error.message} --- [XHR retry message: ${xhrError.message}].`);
          augmentedError.stack = error.stack; // Preserve original stack trace
          onResult({
            ...result,
            error: augmentedError,
          });
        });
      } else {
        onResult(result);
      }
    });
  }
}
