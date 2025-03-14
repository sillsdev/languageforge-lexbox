import AdminIcon from './AdminIcon.svelte';
import AuthenticatedUserIcon from './AuthenticatedUserIcon.svelte';
import CircleArrowIcon from './CircleArrowIcon.svelte';
import HamburgerIcon from './HamburgerIcon.svelte';
import HomeIcon from './HomeIcon.svelte'
import Icon from './Icon.svelte';
import type { IconSize } from './Icon.svelte';
import LogoutIcon from './LogoutIcon.svelte';
import PencilIcon from './PencilIcon.svelte';
import TrashIcon from './TrashIcon.svelte';
import UserAddOutline from './UserAddOutline.svelte';
import type {IconClass} from '../../../viewer/src/lib/icon-class';

export {
  Icon,
  type IconSize,
  AuthenticatedUserIcon,
  CircleArrowIcon,
  HamburgerIcon,
  LogoutIcon,
  UserAddOutline,
  AdminIcon,
  HomeIcon,
  PencilIcon,
  TrashIcon
}

type DaisySize = 'xs' | 'sm' | 'md' | 'lg';
export type IconString = IconClass | `loading loading-spinner${'' | ` loading-${DaisySize}`}`;
