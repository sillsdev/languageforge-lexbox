import {describe, expect, it} from 'vitest';
import {homographSubscript, parseSubscriptNumbers} from './homograph-subscript';

describe('parseSubscriptNumbers', () => {
  it('splits homograph and sense numbers out of a label', () => {
    expect(parseSubscriptNumbers('Apfel₂ › apple₁₂')).toEqual(['Apfel', 2, ' › apple', 12]);
  });

  it('round-trips homographSubscript', () => {
    expect(parseSubscriptNumbers('bank' + homographSubscript(3))).toEqual(['bank', 3]);
  });

  it('leaves text without subscripts alone', () => {
    expect(parseSubscriptNumbers('5.2 Food')).toEqual(['5.2 Food']);
  });

  it('keeps runs that would not round-trip as text', () => {
    expect(parseSubscriptNumbers('H₀ x₀₁')).toEqual(['H', '₀', ' x', '₀₁']);
  });
});
