/* eslint-disable */
// @ts-nocheck — injected verbatim into plugin iframes, not app code.
// FW Lite plugin SDK. This file is injected verbatim (via ?raw import) into every plugin's
// sandboxed iframe, so it must stay plain, dependency-free, widely-supported JavaScript.
// It talks to the host app exclusively through postMessage; see plugin-api-types.ts for the protocol.
(function () {
  'use strict';
  if (window.fwlite) return;

  var pending = new Map();
  var nextId = 1;
  var context = null;
  var launchContext = {};
  var initResolve;
  var ready = new Promise(function (resolve) { initResolve = resolve; });

  window.addEventListener('message', function (event) {
    if (event.source !== window.parent) return;
    var data = event.data;
    if (!data || data.source !== 'fwlite-plugin-host') return;
    if (data.kind === 'init') {
      if (context) return;
      launchContext = data.context || {};
      context = {
        apiVersion: data.apiVersion,
        project: data.project,
        theme: data.theme,
        permissions: data.permissions,
      };
      initResolve(context);
      return;
    }
    if (data.kind === 'response') {
      var handler = pending.get(data.id);
      if (!handler) return;
      pending.delete(data.id);
      if (data.ok) {
        handler.resolve(data.result);
      } else {
        var error = new Error((data.error && data.error.message) || 'Unknown plugin API error');
        error.code = (data.error && data.error.code) || 'internal';
        handler.reject(error);
      }
    }
  });

  function call(method, args) {
    return ready.then(function () {
      return new Promise(function (resolve, reject) {
        var id = nextId++;
        pending.set(id, {resolve: resolve, reject: reject});
        window.parent.postMessage({source: 'fwlite-plugin', v: 1, kind: 'request', id: id, method: method, args: args}, '*');
      });
    });
  }

  window.fwlite = {
    apiVersion: 1,
    /** Resolves once the host has connected. All API calls implicitly wait for this. */
    ready: ready,
    get project() { return context && context.project; },
    get theme() { return context && context.theme; },
    get permissions() { return (context && context.permissions) || []; },
    /** Launch context; `context.entryId` is set when the user opened this plugin from an entry, else absent. */
    get context() { return launchContext; },

    getWritingSystems: function () { return call('getWritingSystems', []); },
    getEntries: function (query) { return call('getEntries', [query || {}]); },
    countEntries: function (query) { return call('countEntries', [query || {}]); },
    getEntry: function (id) { return call('getEntry', [id]); },
    getPartsOfSpeech: function () { return call('getPartsOfSpeech', []); },
    getSemanticDomains: function () { return call('getSemanticDomains', []); },

    createEntry: function (entry) { return call('createEntry', [entry]); },
    updateEntry: function (before, after) { return call('updateEntry', [before, after]); },

    openEntry: function (entryId) { return call('openEntry', [entryId]); },
    notify: function (message) { return call('notify', [message]); },

    storage: {
      get: function (key) { return call('storageGet', [key]); },
      set: function (key, value) { return call('storageSet', [key, value]); },
      remove: function (key) { return call('storageRemove', [key]); },
    },

    /** Flattens a rich string ({spans: [{text}]}) or plain string to text. */
    asText: function (value) {
      if (value == null) return '';
      if (typeof value === 'string') return value;
      if (Array.isArray(value.spans)) {
        return value.spans.map(function (span) { return span.text || ''; }).join('');
      }
      return '';
    },

    /** Picks the first non-empty value from a multistring, preferring the given writing systems. */
    firstValue: function (multiString, preferredWs) {
      if (!multiString) return '';
      var wsList = (preferredWs || []).concat(Object.keys(multiString));
      for (var i = 0; i < wsList.length; i++) {
        var text = window.fwlite.asText(multiString[wsList[i]]);
        if (text) return text;
      }
      return '';
    },
  };

  window.parent.postMessage({source: 'fwlite-plugin', v: 1, kind: 'sdk-ready'}, '*');
})();
