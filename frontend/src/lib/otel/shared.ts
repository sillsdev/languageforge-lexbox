import { get_user_id, user } from '$lib/user';
import {
	SpanStatusCode,
	trace,
	type Exception,
	type Span,
	type Attributes,
} from '@opentelemetry/api';
import type { Cookies, NavigationEvent, RequestEvent } from '@sveltejs/kit';
import { get } from 'svelte/store';
import { SemanticAttributes } from '@opentelemetry/semantic-conventions';

export type ErrorAttributes = Attributes & { ['app.error.source']: ErrorSource };

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
	event: RequestEvent | NavigationEvent | Event,
	metadata: ErrorAttributes,
): string =>
	forActiveOrNewSpan(serviceName, 'error', (span) =>
		recordErrorEvent(span, error, event, metadata),
	);

const recordErrorEvent = (
	span: Span,
	error: unknown,
	event: RequestEvent | NavigationEvent | Event,
	metadata: ErrorAttributes,
) => {
	span.recordException(error as Exception);
	span.setStatus({ code: SpanStatusCode.ERROR });
	if (metadata) {
		span.setAttributes(metadata);
	}
	const userId = get(user)?.id;
	if (userId) {
		span.setAttribute('app.user.id', userId);
	}

	traceEventAttributes(span, event);
	return span.spanContext().traceId;
};

export const traceHeaders = (span: Span, type: 'request' | 'response', headers: Headers) => {
	headers.forEach((value, key) => {
		const normalized_name = normalizeHeaderName(key);
		if (recordedHeaders_normalized.includes(normalized_name)) {
			span.setAttribute(`http.${type}.header.${normalized_name}`, value);
		}
	});
};

export const traceEventAttributes = (span: Span, event: RequestEvent | NavigationEvent | Event) => {
	if (isBrowserEvent(event)) {
		traceBrowserAttributes(span, window);
	} else {
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
			span.setAttribute(
				SemanticAttributes.HTTP_USER_AGENT,
				request.headers.get('User-Agent') ?? '',
			);
			traceHeaders(span, 'request', request.headers);
			span.setAttribute('app.sveltekit.isDataRequest', event.isDataRequest);
		}
	}
};

const traceBrowserAttributes = (span: Span, window: Window) => {
	span.setAttributes({
		['window.location.hostname']: window.location.hostname,
		['window.location.scheme']: window.location.protocol,
		['window.location.href']: window.location.href,
		['window.location.port']: window.location.port,
		['window.location.path']: window.location.pathname,
		['window.location.query']: window.location.search,
		['window.location.hash']: window.location.hash,
		['window.location.origin']: window.location.origin,
		['window.navigator.user_agent']: window.navigator.userAgent,
		['window.navigator.language']: window.navigator.language,
		['window.navigator.languages']: window.navigator.languages.join(),
	});
};

const forActiveOrNewSpan = (serviceName: string, name: string, action: (span: Span) => string) => {
	const activeSpan = trace.getActiveSpan();
	if (activeSpan) {
		return action(activeSpan);
	} else {
		return trace.getTracer(serviceName).startActiveSpan(name, (span) => {
			try {
				action(span);
				return span.spanContext().traceId;
			} catch (error) {
				console.error('Error while processing span', error);
				return span.spanContext().traceId;
			} finally {
				span.end();
			}
		});
	}
};

const trySetUserIdAttribute = (span: Span, cookies: Cookies) => {
	const userId = get_user_id(cookies);
	if (userId) {
		span.setAttribute('app.user.id', userId);
	}
};

const isBrowserEvent = (event: RequestEvent | NavigationEvent | Event): event is Event => {
	return 'bubbles' in event;
};

const isRequestEvent = (event: RequestEvent | NavigationEvent): event is RequestEvent => {
	return 'cookies' in event;
};
