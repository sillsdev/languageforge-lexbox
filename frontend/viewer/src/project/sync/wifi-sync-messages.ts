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
