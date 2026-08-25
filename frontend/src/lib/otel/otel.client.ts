import {ATTR_SERVICE_NAME, ATTR_SERVICE_VERSION} from '@opentelemetry/semantic-conventions';
import {BatchSpanProcessor, WebTracerProvider} from '@opentelemetry/sdk-trace-web';
import {SERVICE_NAME, TRACE_EXPORT_URL_PATTERN, traceUserAttributes} from '.';
import {defaultResource, resourceFromAttributes} from '@opentelemetry/resources';

import {APP_VERSION} from '$lib/util/version';
// We explicitly reference the browser version so that we have proper types
import {OTLPTraceExporter} from '@opentelemetry/exporter-trace-otlp-http/build/src/platform/browser';
import {ZoneContextManager} from '@opentelemetry/context-zone';
import {getWebAutoInstrumentations} from '@opentelemetry/auto-instrumentations-web';
import {instrumentGlobalFetch} from '$lib/util/fetch-proxy';
import {registerInstrumentations} from '@opentelemetry/instrumentation';

export * from '.';

instrumentGlobalFetch(() => {
  registerInstrumentations({
    instrumentations: [getWebAutoInstrumentations({
      '@opentelemetry/instrumentation-document-load': {
        // note: disabling this makes the traceParent in our root layout meaningless
        enabled: false,
      },
      '@opentelemetry/instrumentation-user-interaction': {
        enabled: false,
      },
      // Don't trace the exporter's own POSTs to the collector (see TRACE_EXPORT_URL_PATTERN).
      // traceFetch skips them too; both span-creating layers must, or we loop.
      '@opentelemetry/instrumentation-fetch': {
        ignoreUrls: [TRACE_EXPORT_URL_PATTERN],
      },
      '@opentelemetry/instrumentation-xml-http-request': {
        ignoreUrls: [TRACE_EXPORT_URL_PATTERN],
      },
    })],
  });
});

const resource = defaultResource().merge(
  resourceFromAttributes({
    [ATTR_SERVICE_NAME]: SERVICE_NAME,
    [ATTR_SERVICE_VERSION]: APP_VERSION,
  }),
)

const exporter = new OTLPTraceExporter({
  url: '/v1/traces'
});

const provider = new WebTracerProvider({
  resource: resource,
  spanProcessors: [
    {
      forceFlush: () => Promise.resolve(),
      onStart: (span) => {
        traceUserAttributes(span);
      },
      onEnd: () => {},
      shutdown: () => Promise.resolve(),
    },
    new BatchSpanProcessor(exporter, {
      // max number of spans pulled from the queue and exported in a single batch
      // exports go over fetch (keepAlive), so we're not bound by the old sendBeacon() size limit
      maxExportBatchSize: 30,
      // minimum time between exports
      scheduledDelayMillis: 1000,
      maxQueueSize: 5000, // default: 2048
    }),
  ],
});

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
