import {formatNumber} from './format-number';

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
