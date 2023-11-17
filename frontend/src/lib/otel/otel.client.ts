import { BatchSpanProcessor, WebTracerProvider } from '@opentelemetry/sdk-trace-web'

import { APP_VERSION } from '$lib/util/version';
import { OTLPTraceExporterBrowserWithXhrRetry } from './trace-exporter-browser-with-xhr-retry';
import { Resource } from '@opentelemetry/resources'
import { SERVICE_NAME } from '.'
import { SemanticResourceAttributes } from '@opentelemetry/semantic-conventions'
import { ZoneContextManager } from '@opentelemetry/context-zone'
import { getWebAutoInstrumentations } from '@opentelemetry/auto-instrumentations-web'
import { instrumentGlobalFetch } from '$lib/util/fetch-proxy';
import { registerInstrumentations } from '@opentelemetry/instrumentation'

export * from '.';

instrumentGlobalFetch(() => {
  registerInstrumentations({
    instrumentations: [getWebAutoInstrumentations()],
  });
});

const resource = Resource.default().merge(
  new Resource({
    [SemanticResourceAttributes.SERVICE_NAME]: SERVICE_NAME,
    [SemanticResourceAttributes.SERVICE_VERSION]: APP_VERSION,
  }),
)
const provider = new WebTracerProvider({
  resource: resource,
});
const exporter = new OTLPTraceExporterBrowserWithXhrRetry({
  url: '/v1/traces'
});
provider.addSpanProcessor(
  new BatchSpanProcessor(exporter, {
    // max number of spans pulled from the qeuue and exported in a single batch
    // 30 is often too big for the sendBeacon() API, but we have a fallback to XHR.
    maxExportBatchSize: 30,
    // minimum time between exports
    scheduledDelayMillis: 1000,
    maxQueueSize: 5000, // default: 2048
  }),
);

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
});
