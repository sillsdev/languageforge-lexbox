import { get_user_id, user } from '$lib/user';
import {
	SpanStatusCode,
	trace,
	type Exception,
	type Span,
} from '@opentelemetry/api';
import type { Cookies, NavigationEvent,  RequestEvent } from '@sveltejs/kit';
import { get } from 'svelte/store';
import { SemanticAttributes } from '@opentelemetry/semantic-conventions';

// Span and attribute names and values are based primarily on the OpenTelemetry semantic conventions for HTTP
// https://opentelemetry.io/docs/reference/specification/trace/semantic_conventions/http/

const normalizeHeaderName = (name: string): string => {
	// https://opentelemetry.io/docs/reference/specification/trace/semantic_conventions/http/#http-request-and-response-headers
	return name.replaceAll('-', '_').toLowerCase();
};

const recordedHeaders_normalized = [
	'Accept',
	'Accept-Encoding',
	'Accept-Language',
	'Cache-Control',
	'Pragma',
	'Connection',
	'Content-Length',
	'Contnt-Type',
	'Host',
	'Origin',
	'Referer',
	'Upgrade-Insecure-Requests',
	'User-Agent',
	'Via',
	'ETag',
	'Sec-CH-UA',
	'Sec-CH-UA-Mobile',
	'Sec-CH-UA-Platform',
	'Sec-Fetch-Dest',
	'Sec-Fetch-Mode',
	'Sec-Fetch-Site',
	'Sec-Fetch-User',
	'x_sveltekit_page',
].map(normalizeHeaderName);

export const traceErrorEvent = (
	serviceName: string,
	error: unknown,
	event: RequestEvent | NavigationEvent,
) => {
	trace.getTracer(serviceName).startActiveSpan('error', (span) => {
		span.recordException(error as Exception);
		span.setStatus({ code: SpanStatusCode.ERROR });
		const userId = get(user)?.id;
		if (userId) {
			span.setAttribute('app.user.id', userId);
		}
		traceEventAttributes(span, event);
		span.end();
	});
};

export const traceHeaders = (span: Span, type: string, headers: Headers) => {
	headers.forEach((value, key) => {
		const normalized_name = normalizeHeaderName(key);
		if (recordedHeaders_normalized.includes(normalized_name)) {
			span.setAttribute(`http.${type}.header.${normalized_name}`, value);
		}
	});
};

export const traceEventAttributes = (span: Span, event: RequestEvent | NavigationEvent) => {
	const { route, url } = event;
	span.setAttribute(SemanticAttributes.HTTP_ROUTE, route.id as string);
	span.setAttribute(SemanticAttributes.HTTP_URL, url.href);
	span.setAttribute(
		SemanticAttributes.HTTP_TARGET,
		`${url.pathname}${url.hash ?? ''}${url.search ?? ''}`,
	);
	span.setAttribute(SemanticAttributes.HTTP_SCHEME, url.protocol);
	span.setAttribute(SemanticAttributes.NET_HOST_NAME, url.hostname);
	span.setAttribute(SemanticAttributes.NET_HOST_PORT, url.port);
	if (isRequestEvent(event)) {
		const { request, cookies } = event;
		trySetUserIdAttribute(span, cookies);
		span.setAttribute(SemanticAttributes.HTTP_CLIENT_IP, event.getClientAddress());
		span.setAttribute(SemanticAttributes.HTTP_METHOD, request.method);
		span.setAttribute(
			SemanticAttributes.HTTP_REQUEST_CONTENT_LENGTH,
			request.headers.get('Content-Length') ?? 0,
		);
		span.setAttribute(SemanticAttributes.HTTP_USER_AGENT, request.headers.get('User-Agent') ?? '');
		traceHeaders(span, 'request', request.headers);
		span.setAttribute('app.sveltekit.isDataRequest', event.isDataRequest);
	}
};

const trySetUserIdAttribute = (span: Span, cookies: Cookies) => {
	const userId = get_user_id(cookies);
	if (userId) {
		span.setAttribute('app.user.id', userId);
	}
};

const isRequestEvent = (event: RequestEvent | NavigationEvent): event is RequestEvent => {
	return 'cookies' in event;
};
