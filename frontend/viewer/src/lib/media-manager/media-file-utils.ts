import type {IHarmonyResource} from '$lib/dotnet-types/generated-types/SIL/Harmony/Resource/IHarmonyResource';
import {formatNumber} from '$lib/components/ui/format';
import {gt, locale} from 'svelte-i18n-lingui';
import {fromStore} from 'svelte/store';

export type MediaFileLocation = 'local' | 'remote' | 'both';

export function mediaFileLocation(resource: IHarmonyResource): MediaFileLocation {
  if (resource.local && resource.remote) return 'both';
  if (resource.local) return 'local';
  return 'remote';
}

const currentLocale = fromStore(locale);

export function mediaFileLocationLabel(location: MediaFileLocation): string {
  void currentLocale.current; // invalidate when the current locale changes
  switch (location) {
    case 'local':
      return gt`Local only`;
    case 'remote':
      return gt`Remote only`;
    case 'both':
      return gt`Local and remote`;
  }
}

/** Last segment of a POSIX or Windows path, or undefined when the path has no segment. */
export function basename(path: string): string | undefined {
  return path.split(/[/\\]/).pop() || undefined;
}

/** Best human-readable name for a resource: server metadata, then the cached filename, then its id. */
export function mediaFileDisplayName(resource: IHarmonyResource): string {
  return resource.metadata?.filename
    ?? (resource.localPath ? basename(resource.localPath) : undefined)
    ?? resource.id;
}

const BYTE_UNITS = ['B', 'KB', 'MB', 'GB', 'TB'];

export function formatFileSize(bytes?: number): string | undefined {
  if (bytes == null) return undefined;
  let size = bytes;
  let unit = 0;
  while (size >= 1024 && unit < BYTE_UNITS.length - 1) {
    size /= 1024;
    unit++;
  }
  return `${formatNumber(size, {maximumFractionDigits: unit === 0 ? 0 : 1})} ${BYTE_UNITS[unit]}`;
}

const MIME_TYPE_BY_EXTENSION: Record<string, string> = {
  '.mp3': 'audio/mpeg',
  '.wav': 'audio/wav',
  '.ogg': 'audio/ogg',
  '.webm': 'audio/webm',
  '.m4a': 'audio/mp4',
  '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg',
  '.png': 'image/png',
  '.gif': 'image/gif',
  '.webp': 'image/webp',
  '.svg': 'image/svg+xml',
  '.mp4': 'video/mp4',
  '.pdf': 'application/pdf',
};

export function guessMimeType(filename: string): string {
  const extension = filename.slice(filename.lastIndexOf('.')).toLowerCase();
  return MIME_TYPE_BY_EXTENSION[extension] ?? 'application/octet-stream';
}
