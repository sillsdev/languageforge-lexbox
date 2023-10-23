import { BatchSpanProcessor, WebTracerProvider } from '@opentelemetry/sdk-trace-web'
import { SERVICE_NAME, ensureErrorIsTraced, tracer } from '.'

import {APP_VERSION} from '$lib/util/verstion';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http'
import { Resource } from '@opentelemetry/resources'
import { SemanticResourceAttributes } from '@opentelemetry/semantic-conventions'
import { ZoneContextManager } from '@opentelemetry/context-zone'
import { getWebAutoInstrumentations } from '@opentelemetry/auto-instrumentations-web'
import { instrumentGlobalFetch } from '$lib/util/fetch-proxy';
import { isRedirect } from '$lib/util/types';
import { registerInstrumentations } from '@opentelemetry/instrumentation'

export * from '.';


/**
 * Very minimal instrumentation here, because the auto-instrumentation handles the core stuff,
 * we just want to make sure that our trace-ID gets used and that we stamp errors with it.
 */
export function traceFetch(fetch: () => ReturnType<Fetch>): ReturnType<Fetch> {
  return tracer().startActiveSpan('fetch', async (span) => {
    try {
      return await fetch();
    } catch (error) {
      if (!isRedirect(error)) {
        ensureErrorIsTraced(error, {span}, {['app.error.source']: 'client-fetch-error'});
      }
      throw error;
    } finally {
      span.end();
    }
  });
}

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
    scheduledDelayMillis: 1000,
    maxQueueSize: 5000, // default: 2048
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
