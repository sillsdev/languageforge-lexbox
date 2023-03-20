import { BatchSpanProcessor, WebTracerProvider } from '@opentelemetry/sdk-trace-web'

import type { HandleClientError } from '@sveltejs/kit'
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http'
import { Resource } from '@opentelemetry/resources'
import { SemanticResourceAttributes } from '@opentelemetry/semantic-conventions'
import { ZoneContextManager } from '@opentelemetry/context-zone'
import { getWebAutoInstrumentations } from '@opentelemetry/auto-instrumentations-web'
import { registerInstrumentations } from '@opentelemetry/instrumentation'
import { trace_error_event } from '$lib/otel'

export const handleError: HandleClientError = ({ error, event }) => {
	trace_error_event(service_name, error, event)
}

// OpenTelemetry setup
registerInstrumentations({
	instrumentations: [getWebAutoInstrumentations()],
})
const service_name = 'LexBox-SvelteKit-Client'
const resource = Resource.default().merge(
	new Resource({
		[SemanticResourceAttributes.SERVICE_NAME]: service_name,
		[SemanticResourceAttributes.SERVICE_VERSION]: '0.0.1',
	}),
)
const provider = new WebTracerProvider({
	resource: resource,
})
const exporter = new OTLPTraceExporter()
provider.addSpanProcessor(
	new BatchSpanProcessor(exporter, {
		// this is somewhat sensitive
		maxExportBatchSize: 30,
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
