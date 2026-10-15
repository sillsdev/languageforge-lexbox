import {
  CHANNEL_FLAGS,
  channelHasFlag,
  isDevChannel,
  persistChannel,
  readStoredChannel,
  type FeatureFlag,
} from './feature-flags';

export type {FeatureFlag};

class FeatureFlagState {
  #channel = $state(readStoredChannel());

  get channel(): string {
    return this.#channel;
  }

  set channel(value: string) {
    this.#channel = value;
    persistChannel(value);
  }

  get isDev(): boolean {
    return isDevChannel(this.#channel);
  }

  hasFlag(flag: FeatureFlag): boolean {
    return channelHasFlag(flag, this.#channel, CHANNEL_FLAGS);
  }
}

export const featureFlags = new FeatureFlagState();

export function hasFlag(flag: FeatureFlag): boolean {
  return featureFlags.hasFlag(flag);
}
