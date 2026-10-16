import {randomId} from '$lib/utils';

// Names for files FW Lite creates itself (recordings, camera captures). Never put vernacular text
// in them: field ids and ws tags are plain ASCII, so the result needs no sanitizing.
// e.g. lexemeForm_sen_20260928-1357_a7f3c91d.webm
// The random suffix is load-bearing, not decoration: both save paths treat the filename as the
// file's identity and return the existing file when the name is already taken
// (FwDataMiniLcmApi.SaveFile, LcmMediaService.SaveFile), so a collision silently attaches the
// older recording instead of the new one.
export function generatedMediaFilename(
  {field, ws}: {field?: string; ws?: string},
  extension: string,
  now = new Date(),
): string {
  const parts = [field, ws && stripAudioTag(ws), timestamp(now), randomId().replace(/-/g, '').slice(0, 8)];
  return `${parts.filter(Boolean).join('_')}.${extension.replace(/^\./, '')}`;
}

// An audio writing system is the Zxxx script plus an `audio` private-use subtag, which can sit
// anywhere in the variants (e.g. seh-Zxxx-x-audio-var) - see MiniLcm's WritingSystemId.IsAudio.
// None of those subtags say anything the file extension doesn't.
function stripAudioTag(ws: string): string {
  const subtags = ws.split('-');
  const isAudio = subtags.some((s) => /^Zxxx$/i.test(s)) && subtags.some((s) => /^audio$/i.test(s));
  if (!isAudio) return ws;
  return subtags.filter((s) => !/^(Zxxx|x|audio)$/i.test(s)).join('-');
}

function pad(n: number): string {
  return n.toString().padStart(2, '0');
}

function timestamp(date: Date): string {
  return `${date.getFullYear()}${pad(date.getMonth() + 1)}${pad(date.getDate())}-${pad(date.getHours())}${pad(date.getMinutes())}`;
}
