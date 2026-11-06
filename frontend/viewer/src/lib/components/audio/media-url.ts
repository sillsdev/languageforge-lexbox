/**
 * Mac Catalyst's WKWebView never loads media from blob: URLs (readyState stays 0 and play() never settles),
 * while data: URLs play fine. Probe once with a tiny in-memory WAV and fall back to data: URLs where blobs don't load.
 */
let blobMediaWorks: Promise<boolean> | undefined;

export function blobMediaUrlsWork(): Promise<boolean> {
  return (blobMediaWorks ??= probeBlobMedia());
}

/** A URL an <audio> element can play. Revoking it with URL.revokeObjectURL is safe either way (a no-op for data: URLs). */
export async function createMediaUrl(blob: Blob): Promise<string> {
  if (await blobMediaUrlsWork()) return URL.createObjectURL(blob);
  return await new Promise<string>((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(reader.result as string);
    reader.onerror = () => reject(reader.error ?? new Error('Failed to read audio blob'));
    reader.readAsDataURL(blob);
  });
}

function probeBlobMedia(timeoutMs = 1500): Promise<boolean> {
  const url = URL.createObjectURL(new Blob([silentWav()], {type: 'audio/wav'}));
  const audio = new Audio();
  return new Promise<boolean>((resolve) => {
    function done(works: boolean) {
      clearTimeout(timer);
      audio.removeAttribute('src');
      URL.revokeObjectURL(url);
      resolve(works);
    }
    const timer = setTimeout(() => done(false), timeoutMs);
    audio.onloadedmetadata = () => done(true);
    audio.onerror = () => done(false);
    audio.preload = 'metadata';
    audio.src = url;
  });
}

/** 0.1s of 8kHz 16-bit mono silence. */
function silentWav(): ArrayBuffer {
  const sampleRate = 8000;
  const dataSize = (sampleRate / 10) * 2;
  const view = new DataView(new ArrayBuffer(44 + dataSize));
  function ascii(offset: number, text: string) {
    [...text].forEach((c, i) => view.setUint8(offset + i, c.charCodeAt(0)));
  }
  ascii(0, 'RIFF');
  view.setUint32(4, 36 + dataSize, true);
  ascii(8, 'WAVE');
  ascii(12, 'fmt ');
  view.setUint32(16, 16, true); // fmt chunk size
  view.setUint16(20, 1, true); // PCM
  view.setUint16(22, 1, true); // mono
  view.setUint32(24, sampleRate, true);
  view.setUint32(28, sampleRate * 2, true); // byte rate
  view.setUint16(32, 2, true); // block align
  view.setUint16(34, 16, true); // bits per sample
  ascii(36, 'data');
  view.setUint32(40, dataSize, true);
  return view.buffer;
}
