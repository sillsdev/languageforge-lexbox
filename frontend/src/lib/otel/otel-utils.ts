import { get_user_id, user } from '$lib/user'
import { SpanStatusCode, trace, TraceFlags, type Exception, type Span } from '@opentelemetry/api'
import type { Cookies, NavigationEvent, Redirect, RequestEvent } from '@sveltejs/kit'
import { get } from 'svelte/store'

export const get_trace_parent = (): string | undefined => {
	const spanContext = trace.getActiveSpan()?.spanContext()
	if (!spanContext) {
		return
	}
	const sampleDecision = Number(spanContext.traceFlags || TraceFlags.NONE).toString(16)
	return `00-${spanContext.traceId}-${spanContext.spanId}-0${sampleDecision}`
}

export const trace_request = async (
	service_name: string,
	event: RequestEvent,
	response_builder: () => Promise<Response> | Response,
): Promise<Response> => {
	return trace.getTracer(service_name).startActiveSpan('handle-request', async (span) => {
		traceEventAttributes(span, event)
		const response = await response_builder()
		span.end()
		return response
	})
}

export const trace_response = async (
	serviceName: string,
	response_builder: () => Promise<Response> | Response,
): Promise<Response> => {
	return trace.getTracer(serviceName).startActiveSpan('create-response', async (responseSpan) => {
		try {
			const response = await response_builder()
			trace_response_attributes(responseSpan, response)
			return response
		} catch (error) {
			if (is_redirect(error)) {
				traceRedirectAttributes(responseSpan, error)
			}
			throw error
		} finally {
			responseSpan.end()
		}
	})
}

export const trace_error_event = (
	serviceName: string,
	error: unknown,
	event: RequestEvent | NavigationEvent,
) => {
	trace.getTracer(serviceName).startActiveSpan('error', (span) => {
		span.recordException(error as Exception)
		span.setStatus({ code: SpanStatusCode.ERROR })
		const userId = get(user)?.id
		if (userId) {
			span.setAttribute('app.user.id', userId)
		}
		traceEventAttributes(span, event)
		span.end()
	})
}

const traceRedirectAttributes = (span: Span, redirect: Redirect) => {
	span.setAttribute('http.redirect_location', redirect.location)
	span.setAttribute('http.status_code', redirect.status)
}

const traceEventAttributes = (span: Span, event: RequestEvent | NavigationEvent) => {
	span.setAttribute('http.route', event.route.id as string)
	span.setAttribute('http.url', event.url.href)
	span.setAttribute(
		'http.target',
		`${event.url.pathname}${event.url.hash ?? ''}${event.url.search ?? ''}`,
	)
	span.setAttribute('http.scheme', event.url.protocol)
	span.setAttribute('http.host.name', event.url.hostname)
	span.setAttribute('http.host.port', event.url.port)
	if (is_request_event(event)) {
		try_set_user_id_attribute(span, event.cookies)
		span.setAttribute('http.client_ip', event.getClientAddress())
		span.setAttribute('http.method', event.request.method)
		span.setAttribute(
			'http.request_content_length',
			event.request.headers.get('Content-Length') ?? 0,
		)
		span.setAttribute('user_agent.original', event.request.headers.get('User-Agent') ?? '')
		event.request.headers.forEach((value, key) => {
			span.setAttribute(`http.request.header.${key}`, value)
		})
		span.setAttribute('app.sveltekit.isDataRequest', event.isDataRequest)
	}
}

const trace_response_attributes = (span: Span, response: Response) => {
	span.setAttribute('http.status_code', response.status)
	span.setAttribute('http.response_content_length', response.headers.get('Content-Length') ?? 0)
	response.headers.forEach((value, key) => {
		span.setAttribute(`http.response.header.${key}`, value)
	})
}

const try_set_user_id_attribute = (span: Span, cookies: Cookies) => {
	span.setAttribute('app.user_id', get_user_id(cookies) ?? '')
}

const is_request_event = (event: RequestEvent | NavigationEvent): event is RequestEvent => {
	return 'cookies' in event
}

const is_redirect = (error: any): error is Redirect => {
	return 'status' in error && 'location' in error
}
