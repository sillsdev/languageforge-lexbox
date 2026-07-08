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
        capabilities: data.capabilities || {openEntryModes: []},
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
    /** What this host supports, so you can feature-detect. e.g. capabilities.openEntryModes. */
    get capabilities() { return (context && context.capabilities) || {openEntryModes: []}; },
    /** Launch context; `context.entryId` is set when the user opened this plugin from an entry, else absent. */
    get context() { return launchContext; },

    getWritingSystems: function () { return call('getWritingSystems', []); },
    getEntries: function (query) { return call('getEntries', [query || {}]); },
    countEntries: function (query) { return call('countEntries', [query || {}]); },
    getEntry: function (id) { return call('getEntry', [id]); },
    getPartsOfSpeech: function () { return call('getPartsOfSpeech', []); },
    getSemanticDomains: function () { return call('getSemanticDomains', []); },

    /**
     * Fetches the bytes for a media reference — an audio writing-system value (from an audio ws in
     * lexemeForm/citationForm/example, etc.) or a sense picture's `mediaUri`. The app downloads the
     * file automatically on first use, so this can take a moment and can fail when offline.
     * Resolves to {data: ArrayBuffer, fileName, mimeType} or null (offline / not found).
     * Turn it into something playable/showable in your iframe, e.g.:
     *   var m = await fwlite.getMedia(uri);
     *   if (m) audio.src = URL.createObjectURL(new Blob([m.data], {type: m.mimeType || ''}));
     */
    getMedia: function (mediaUri) { return call('getMedia', [mediaUri]); },

    /**
     * Stores a file (e.g. audio you recorded with MediaRecorder, or a captured image) and resolves
     * to {result, mediaUri, errorMessage}. Then attach it by writing `mediaUri` into an entry field
     * (an audio writing system, or a sense picture) via updateEntry — that edit is user-approved.
     * The browser prompts the user the first time you call getUserMedia for mic/camera.
     *   const blob = /* from MediaRecorder * /;
     *   const {mediaUri} = await fwlite.saveFile(await blob.arrayBuffer(), {filename: 'word.webm', mimeType: blob.type});
     */
    saveFile: function (data, metadata) { return call('saveFile', [data, metadata]); },

    createEntry: function (entry) { return call('createEntry', [entry]); },
    updateEntry: function (before, after) { return call('updateEntry', [before, after]); },

    /**
     * Applies several changes behind a SINGLE approval dialog (instead of one dialog per write).
     * Pass an array of {type:'createEntry', entry} and/or {type:'updateEntry', before, after}.
     * Resolves to the resulting entries in order; rejects with code 'permission-denied' if the user declines.
     */
    applyChanges: function (operations) { return call('applyChanges', [operations]); },

    /**
     * Opens an entry. options.mode: 'view' (default, read-only dialog), 'edit' (editable dialog),
     * 'window' (separate window; falls back to 'view' where unsupported), or 'navigate' (leaves the
     * plugin — loses plugin state). 'view'/'edit'/'window' keep your plugin running underneath.
     */
    openEntry: function (entryId, options) { return call('openEntry', [entryId, options || {}]); },
    notify: function (message) { return call('notify', [message]); },

    /**
     * Comments (read-only). subjectType is 'Entry' | 'Sense' | 'ExampleSentence'.
     * getUnreadComments()/countUnreadComments() with no threadId cover the whole project (read
     * status is per-device). A thread groups comments about one subject; getUserComments lists them.
     */
    getCommentThreads: function (options) { return call('getCommentThreads', [options]); },
    getCommentThread: function (threadId) { return call('getCommentThread', [threadId]); },
    getUserComments: function (threadId) { return call('getUserComments', [threadId]); },
    getUnreadComments: function (options) { return call('getUnreadComments', [options || {}]); },
    getUnreadCommentsForSubject: function (options) { return call('getUnreadCommentsForSubject', [options]); },
    countUnreadComments: function (options) { return call('countUnreadComments', [options || {}]); },

    /**
     * Activity / history (read-only). getActivity is the project-wide change feed (newest first);
     * getEntityHistory(entityId) is one entry's timeline. Not available for every project type —
     * calls reject with code 'not-supported' when there's no history.
     */
    getActivity: function (query) { return call('getActivity', [query || {}]); },
    getEntityHistory: function (entityId) { return call('getEntityHistory', [entityId]); },
    getChangeContext: function (options) { return call('getChangeContext', [options]); },
    getObjectAtCommit: function (options) { return call('getObjectAtCommit', [options]); },
    listActivityAuthors: function () { return call('listActivityAuthors', []); },
    listActivityChangeTypes: function () { return call('listActivityChangeTypes', []); },

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

    /**
     * The entry's headword the way FW Lite shows it: citation form, else the lexeme form with the
     * morph type's affix markers (e.g. "-s", "un-"). Computed by the app and attached to every
     * entry you receive — use this instead of reading citationForm/lexemeForm yourself.
     */
    headword: function (entry) {
      return (entry && typeof entry.headword === 'string') ? entry.headword : '';
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
