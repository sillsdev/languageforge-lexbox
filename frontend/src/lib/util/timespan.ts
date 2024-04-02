export type TimespanComponent = 'days' | 'hours' | 'minutes' | 'seconds' | 'ms'
export type Timespan = [number, TimespanComponent];

const timespanRe = /^(?:(\d+)\.)?(\d+):(\d+):(\d+)(?:\.(\d+))?$/;

export function parseSimpleTimespan(timespan: string): Timespan {
  const m = timespanRe.exec(timespan);
  if (m) {
    if (m[1] && Number(m[1])) {
      return [Number(m[1]), 'days'];
    }
    if (Number(m[2])) {
      return [Number(m[2]), 'hours'];
    }
    if (Number(m[3])) {
      return [Number(m[3]), 'minutes'];
    }
    if (Number(m[4])) {
      return [Number(m[4]), 'seconds'];
    }
    if (m[5] && Number(m[5])) {
      return [Number(m[5]), 'ms'];
    }
  }
  return [0, 'seconds'];
}
