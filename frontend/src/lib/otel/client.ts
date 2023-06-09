import { BatchSpanProcessor, WebTracerProvider } from '@opentelemetry/sdk-trace-web'

import { getWebAutoInstrumentations } from '@opentelemetry/auto-instrumentations-web'
import { ZoneContextManager } from '@opentelemetry/context-zone'
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http'
import { registerInstrumentations } from '@opentelemetry/instrumentation'
import { Resource } from '@opentelemetry/resources'
import { SemanticResourceAttributes } from '@opentelemetry/semantic-conventions'
import type { NavigationEvent } from '@sveltejs/kit'
import { traceErrorEvent as _traceErrorEvent, type ErrorAttributes, type TraceId } from './shared'

const serviceName = 'LexBox-SvelteKit-Client'

export const traceErrorEvent = (
  error: unknown,
  event: NavigationEvent | Event,
  metadata: ErrorAttributes,
): TraceId => _traceErrorEvent(serviceName, error, event, metadata)

// fetch_original & fetch_otel_instrumented are referenced by our fetch_proxy in app.html
const fetchProxy = window.fetch;
try {
  // Have otel instrument the original
  window.fetch = window.fetch_original;
  registerInstrumentations({
    instrumentations: [getWebAutoInstrumentations()],
  });
} finally {
  // Provide the (now) instrumented version for our proxy to call
  window.fetch_otel_instrumented = window.fetch;
  // Put the proxy back into place
  window.fetch = fetchProxy;
}

const resource = Resource.default().merge(
  new Resource({
    [SemanticResourceAttributes.SERVICE_NAME]: serviceName,
    [SemanticResourceAttributes.SERVICE_VERSION]: '0.0.1',
  }),
)
const provider = new WebTracerProvider({
  resource: resource,
})
const exporter = new OTLPTraceExporter({
  url: '/v1/traces'
});
provider.addSpanProcessor(
  new BatchSpanProcessor(exporter, {
    // max number of spans pulled from the qeuue and exported in a single batch
    // this can't be much higher or the export will be too big for the sendBeacon() API
    maxExportBatchSize: 15,
    // minimum time between exports
    scheduledDelayMillis: 2000,
  }),
)

// Debugging:
// diag.setLogger(new DiagConsoleLogger(), DiagLogLevel.DEBUG)
// provider.addSpanProcessor(new SimpleSpanProcessor(exporter))
// provider.addSpanProcessor(new SimpleSpanProcessor(new ConsoleSpanExporter()))

provider.register({
  // https://opentelemetry.io/docs/instrumentation/js/getting-started/browser/#creating-a-tracer-provider
  // Changing default contextManager to use ZoneContextManager - supports asynchronous operations - optional
  // Apparently shouldn't work due to zone.js if targeting ES2017+: https://github.com/open-telemetry/opentelemetry-js/tree/main/packages/opentelemetry-context-zone-peer-dep#installation
  // E.g.: https://github.com/open-telemetry/opentelemetry-js/issues/3171
  contextManager: new ZoneContextManager(),
})
