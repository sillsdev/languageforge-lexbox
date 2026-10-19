import {IsMobile} from '$lib/hooks/is-mobile.svelte';
import {QueryParamState, QueryParamStateBool} from '$lib/utils/url.svelte';

/**
 * DESKTOP: the detail is a sibling of the master (split view). Switching selection
 * pushes history via the id param.
 * MOBILE: detail is a child of the master (hierarchical). Opening detail pushes
 * history via the open param; the id is replace-only so back closes detail.
 */
export class MasterDetailUrlState {
  readonly selectedId: QueryParamState;
  readonly detailOpen: QueryParamStateBool;

  constructor(idParam: string, openParam: string) {
    this.selectedId = new QueryParamState({
      key: idParam,
      allowBack: !IsMobile.value,
      replaceOnDefaultValue: !IsMobile.value,
    });
    this.detailOpen = new QueryParamStateBool(
      {
        key: openParam,
        allowBack: IsMobile.value,
        replaceOnDefaultValue: IsMobile.value,
      },
      false,
    );
  }

  get id(): string {
    return this.selectedId.current;
  }

  set id(value: string) {
    this.selectedId.current = value;
    this.detailOpen.current = !!value;
  }

  get showMaster(): boolean {
    return !IsMobile.value || !this.selectedId.current || !this.detailOpen.current;
  }

  get showDetail(): boolean {
    return !IsMobile.value || (!!this.selectedId.current && this.detailOpen.current);
  }

  select(id: string): void {
    this.id = id;
  }

  close(): void {
    if (IsMobile.value) {
      // Preserve the selected id so the list can scroll back to it.
      this.detailOpen.current = false;
    }
  }
}
