import {randomId} from '$lib/utils';

// Names for files FW Lite creates itself (recordings, camera captures). Never put vernacular text
// in them: field ids and ws tags are plain ASCII, so the result needs no sanitizing.
// e.g. lexemeForm_sen_20260928-1357_a7f3.webm
export function generatedMediaFilename(
  {field, ws}: {field?: string; ws?: string},
  extension: string,
  now = new Date(),
): string {
  const parts = [field, ws && stripAudioTag(ws), timestamp(now), randomId().slice(0, 4)];
  return `${parts.filter(Boolean).join('_')}.${extension.replace(/^\./, '')}`;
}

// FLEx audio writing systems are `<lang>-Zxxx-x-audio`; the suffix says nothing the extension doesn't.
function stripAudioTag(ws: string): string {
  return ws.replace(/-Zxxx-x-audio$/i, '');
}

function pad(n: number): string {
  return n.toString().padStart(2, '0');
}

function timestamp(date: Date): string {
  return `${date.getFullYear()}${pad(date.getMonth() + 1)}${pad(date.getDate())}-${pad(date.getHours())}${pad(date.getMinutes())}`;
}
