import {watch, type Getter} from 'runed';

export class StompGuard<T> {
  private _dirty = $state(false);
  private _value: T = $state<T>()!;

  constructor(
    private readonly parentGetter: Getter<T>,
    private readonly parentSetter: (value: T) => void,
  ) {
    this._value = parentGetter();
    watch(parentGetter, (newParentValue) => {
      if (newParentValue === this._value) return; // we probably updated the parent

      if (this._dirty) {
        // ignore and revert parent changes
        parentSetter(this._value);
      } else {
        // accept parent changes
        this._value = newParentValue;
      }
    });
  }

  get value(): T {
    if (this._dirty) {
      return this._value;
    }
    // we could always return this._value, but this saves us a tick
    return this.parentGetter();
  }

  set value(newValue: T) {
    this._dirty = true;
    this._value = newValue;
    this.parentSetter(this._value);
  }

  get isDirty(): boolean {
    return this._dirty;
  }

  commitAndUnlock(): void {
    this.assertMatchesParent();
    this._dirty = false;
  }

  private assertMatchesParent(): void {
    const parentValue = this.parentGetter();
    if (parentValue !== this._value) {
      const p = $state.snapshot(parentValue);
      const v = $state.snapshot(this._value);
      if (import.meta.env.DEV) {
        throw new Error(
          `Expected parent value to match the guard value. (${JSON.stringify(p)}) (${JSON.stringify(v)})`,
        );
      } else {
        console.error('Expected parent value to match the guard value', p, v);
        this.parentSetter(this._value); // revert to guard value
      }
    }
  }
}
