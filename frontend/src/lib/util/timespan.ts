export type TimespanComponent = 'days' | 'hours' | 'minutes' | 'seconds' | 'ms'
export type Timespan = [number, TimespanComponent];
export type TimespanI18nKey = `emails.link_expiration_${TimespanComponent}`;
export type TimespanI18nParam = Record<TimespanComponent, number>;
export type TimespanI18nKeyAndParam = [TimespanI18nKey, TimespanI18nParam]
const timespanRe = /^(?:(\d+)\.)?(\d+):(\d+):(\d+)(\.\d+)?$/;

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
      return [Math.floor(Number(m[5])*1000), 'ms'];
    }
  }
  return [0, 'seconds'];
}

export function toI18nKey(timespan: string): TimespanI18nKeyAndParam {
  const [timecount, timetype] = parseSimpleTimespan(timespan);
  const expirationText: TimespanI18nKey = `emails.link_expiration_${timetype}`;
  const expirationParam = {} as TimespanI18nParam;
  expirationParam[timetype] = timecount;
  return [expirationText, expirationParam];
}
