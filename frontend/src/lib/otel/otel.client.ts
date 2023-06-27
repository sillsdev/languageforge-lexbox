import { BatchSpanProcessor, WebTracerProvider } from '@opentelemetry/sdk-trace-web'
import { SERVICE_NAME, ensureErrorIsTraced, isRedirect, tracer } from '.'

import {APP_VERSION} from '$lib/util/verstion';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http'
import { Resource } from '@opentelemetry/resources'
import { SemanticResourceAttributes } from '@opentelemetry/semantic-conventions'
import { ZoneContextManager } from '@opentelemetry/context-zone'
import { getWebAutoInstrumentations } from '@opentelemetry/auto-instrumentations-web'
import { registerInstrumentations } from '@opentelemetry/instrumentation'

export * from '.';

type Fetch = typeof fetch;

let fetchHandler: ((fetch: Fetch, ...args: Parameters<Fetch>) => Promise<Response>) | undefined;

export const handleFetch = (_fetchHandler: (fetch: Fetch, ...args: Parameters<Fetch>) => Promise<Response>): void => {
  if (fetchHandler) throw new Error('OTEL fetch handler was already initialized');
  fetchHandler = _fetchHandler;
};

/**
 * Very minimal instrumentation here, because the auto-instrumentation handles the core stuff,
 * we just want to make sure that our trace-ID gets used and that we stamp errors with it.
 */
export const traceFetch = (fetch: () => ReturnType<Fetch>): ReturnType<Fetch> => {
  return tracer().startActiveSpan('fetch', async (span) => {
    try {
      return await fetch();
    } catch (error) {
      if (!isRedirect(error)) {
        ensureErrorIsTraced(error, { span }, { ['app.error.source']: 'client-fetch-error' });
      }
      throw error;
    } finally {
      span.end();
    }
  });
};

// fetch_original & fetch_instrumented are referenced by our fetch_proxy in app.html
const fetchProxy = window.fetch;
try {
  // Have otel instrument the original
  window.fetch = window.fetch_original;
  registerInstrumentations({
    instrumentations: [getWebAutoInstrumentations()],
  });
} finally {
  // The (now) instrumented version for our proxy to call
  const instrumentedFetch = window.fetch;
  // Wrap it in our own fetch handler/interceptor so we can intercept 401's and such
  // and then provide it to the proxy so it gets used
  window.fetch_instrumented = (...args) => fetchHandler ? fetchHandler(instrumentedFetch, ...args) : instrumentedFetch(...args);
  // Put the proxy back into place
  window.fetch = fetchProxy;
}

const resource = Resource.default().merge(
  new Resource({
    [SemanticResourceAttributes.SERVICE_NAME]: SERVICE_NAME,
    [SemanticResourceAttributes.SERVICE_VERSION]: APP_VERSION,
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
