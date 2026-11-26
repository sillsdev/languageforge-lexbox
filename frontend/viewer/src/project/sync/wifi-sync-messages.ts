import {HostingProblem} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/Hosting/HostingProblem';
import {JoinRejection} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/Hosting/JoinRejection';
import {LocalSyncProblem} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/Hosting/LocalSyncProblem';
import {gt} from 'svelte-i18n-lingui';

export function joinRejectionMessage(rejection: JoinRejection | undefined): string {
  switch (rejection) {
    case JoinRejection.Declined:
      return gt`The other device declined.`;
    case JoinRejection.DifferentHistory:
      return gt`The other device has a separate copy of this project that can't be merged with yours.`;
    case JoinRejection.SessionEnded:
      return gt`The other device stopped sharing.`;
    default:
      return gt`The other device didn't let this one join.`;
  }
}

export function localSyncProblemMessage(problem: LocalSyncProblem): string {
  switch (problem) {
    case LocalSyncProblem.HostUnreachable:
      return gt`Can't reach the other device. Check that you're on its Wi-Fi network. Retrying...`;
    case LocalSyncProblem.CouldNotJoin:
      return gt`Couldn't reach the other device. Check that you're on its Wi-Fi network, then try again.`;
    case LocalSyncProblem.DifferentProject:
      return gt`The other device is sharing a different project.`;
    case LocalSyncProblem.DifferentHistory:
      return gt`The other device has a separate copy of this project that can't be merged with yours.`;
    case LocalSyncProblem.NoWritingSystemInCommon:
      return gt`The other device's copy of this project shares no writing system with yours.`;
    case LocalSyncProblem.Failed:
      return gt`Couldn't sync with the other device. Your changes are safe.`;
  }
}

export function hostingProblemMessage(problem: HostingProblem): string {
  switch (problem) {
    case HostingProblem.WifiOff:
      return gt`Wi-Fi is turned off on this computer. Turn it on, then try again.`;
    case HostingProblem.WifiBusy:
      return gt`Wi-Fi is busy with something else, such as Mobile hotspot. Turn that off, then try again.`;
    case HostingProblem.CantCreateNetwork:
      return gt`This computer couldn't create a Wi-Fi network. Share on a network it's already on instead.`;
    case HostingProblem.CantListen:
      return gt`Couldn't share on that network. Try another one.`;
    case HostingProblem.AlreadyHosting:
      return gt`Another FieldWorks Lite window is already sharing a project. Stop sharing it there first.`;
  }
}
