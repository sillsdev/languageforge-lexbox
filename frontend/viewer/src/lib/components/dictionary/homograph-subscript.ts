const subscriptDigits = '₀₁₂₃₄₅₆₇₈₉';

/**
 * Plain-text form of HomographNumber, for places that only take a string (e.g. a `title`).
 * Display only: never append it to headwords used for search, sorting or matching.
 */
export function homographSubscript(homographNumber: number): string {
  if (!(homographNumber > 0)) return '';
  return String(homographNumber).replace(/\d/g, (digit) => subscriptDigits[Number(digit)]);
}

/**
 * Inverse of {@link homographSubscript}: splits text into plain strings and the numbers its subscript runs encode.
 * A run that wouldn't round-trip (₀, ₀₁) stays text, since HomographNumber would drop or change it.
 */
export function parseSubscriptNumbers(text: string): (string | number)[] {
  return text
    .split(/([₀-₉]+)/u)
    .filter(Boolean)
    .map((part) => {
      const number = Number([...part].map((digit) => subscriptDigits.indexOf(digit)).join(''));
      return homographSubscript(number) === part ? number : part;
    });
}
