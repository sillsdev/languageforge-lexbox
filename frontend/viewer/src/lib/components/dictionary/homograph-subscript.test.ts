import {describe, expect, it} from 'vitest';
import {homographSubscript, splitHomographSubscript} from './homograph-subscript';

describe('splitHomographSubscript', () => {
  it('splits off a trailing homograph number', () => {
    expect(splitHomographSubscript('Apfel₂')).toEqual({text: 'Apfel', homographNumber: 2});
    expect(splitHomographSubscript('bank₁₂')).toEqual({text: 'bank', homographNumber: 12});
  });

  it('round-trips homographSubscript', () => {
    expect(splitHomographSubscript('bank' + homographSubscript(3))).toEqual({text: 'bank', homographNumber: 3});
  });

  it('leaves subscripts that are not a trailing homograph number alone', () => {
    expect(splitHomographSubscript('H₂O')).toEqual({text: 'H₂O', homographNumber: 0});
    expect(splitHomographSubscript('x₀')).toEqual({text: 'x₀', homographNumber: 0});
    expect(splitHomographSubscript('x₀₁')).toEqual({text: 'x₀₁', homographNumber: 0});
    expect(splitHomographSubscript('5.2 Food')).toEqual({text: '5.2 Food', homographNumber: 0});
  });
});
