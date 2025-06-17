import {watch, type Getter} from 'runed';

export class StompGuard<T> {

  private _dirty = false;
  private _value: T = $state<T>()!;

  constructor(private readonly parentGetter: Getter<T>, private readonly parentSetter: (value: T) => void) {
    this._value = parentGetter();
    watch(parentGetter, (newParentValue) => {
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
    return this.parentGetter();
  }

  set value(newValue: T) {
    this._dirty = true;
    this._value = newValue;
    this.parentSetter(this._value);
  }

  commitAndUnlock(): void {
    this.parentSetter(this._value); // maybe redundant or maybe prevents a tick delay
    this._dirty = false;
  }
}
