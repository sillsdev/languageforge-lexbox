import { is_authn } from '$lib/user'
import { redirect, type Handle, type HandleServerError, type ResolveOptions } from '@sveltejs/kit'
import { initClient } from '$lib/graphQLClient'
import { Resource } from '@opentelemetry/resources'
import { SemanticResourceAttributes } from '@opentelemetry/semantic-conventions'
import { NodeSDK } from '@opentelemetry/sdk-node'
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http'
import { trace_error_event, trace_response, trace_request } from '$lib/otel'
import { getNodeAutoInstrumentations } from '@opentelemetry/auto-instrumentations-node'

const public_routes = [
	'/login',
]
const service_name = 'LexBox-SvelteKit'

export const handle = (async ({ event, resolve }) => {
	return trace_request(service_name, event, () => {
		const { cookies, url: { pathname } } = event
		const options: ResolveOptions = {
			filterSerializedResponseHeaders: () => true
		}

		initClient(event)

		return trace_response(service_name, () => {
			if (public_routes.includes(pathname)) {
				return resolve(event, options)
			}
	
			if (!is_authn(cookies)) {
				throw redirect(307, '/login')
			}
	
			return resolve(event, options)
		})
	})
}) satisfies Handle

export const handleError: HandleServerError = ({ error, event }) => {
	trace_error_event(service_name, error, event)
}


// OpenTelemetry setup

// Debugging:
// diag.setLogger(new DiagConsoleLogger(), DiagLogLevel.ALL)
// const traceExporter = new ConsoleSpanExporter()

const traceExporter = new OTLPTraceExporter()
const sdk = new NodeSDK({
	resource: new Resource({
		[SemanticResourceAttributes.SERVICE_NAME]: service_name,
		[SemanticResourceAttributes.SERVICE_VERSION]: '0.0.1',
	}),
	traceExporter,
	instrumentations: [
		// Doesn't seem to do anything in our case (except for fs, which is disabled below)
		getNodeAutoInstrumentations({
			'@opentelemetry/instrumentation-fs': {
				// generates hundreds of thousands of spans
				enabled: false,
			},
		}),
	],
})

sdk.start()

// gracefully shut down the SDK on process exit
process.on('SIGTERM', () => {
	sdk
		.shutdown()
		.then(() => console.log('Tracing terminated'))
		.catch((error) => console.log('Error terminating tracing', error))
		.finally(() => process.exit(0))
})
