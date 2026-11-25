import type {ISyncServiceJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/ISyncServiceJsInvokable';
import type {IJoinSessionView} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/Hosting/IJoinSessionView';
import {HostingProblem} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/Hosting/HostingProblem';
import {JoinSessionState} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/Hosting/JoinSessionState';

type WifiSyncMethods = Pick<ISyncServiceJsInvokable,
  'getHostingOptions' | 'getHosting' | 'startHosting' | 'stopHosting' | 'approveJoiner' | 'declineJoiner' | 'setAutoApprove'
  | 'findNearbyHosts' | 'joinHost' | 'getJoinSession' | 'syncWithHostNow' | 'leaveHost'>;

const notJoined: IJoinSessionView = {state: JoinSessionState.NotJoined};

/** Wi-Fi sync for fakes that don't exercise it: not hosting, not joined, nobody nearby. */
export const idleWifiSync: WifiSyncMethods = {
  getHostingOptions: () => Promise.resolve({canCreateNetwork: false, networks: []}),
  getHosting: () => Promise.resolve(undefined),
  startHosting: () => Promise.resolve({problem: HostingProblem.CantCreateNetwork}),
  stopHosting: () => Promise.resolve(),
  approveJoiner: () => Promise.resolve(),
  declineJoiner: () => Promise.resolve(),
  setAutoApprove: () => Promise.resolve(),
  findNearbyHosts: () => Promise.resolve([]),
  joinHost: () => Promise.resolve(notJoined),
  getJoinSession: () => Promise.resolve(notJoined),
  syncWithHostNow: () => Promise.resolve(notJoined),
  leaveHost: () => Promise.resolve(notJoined),
};
