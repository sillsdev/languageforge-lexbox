import type { PartOfSpeech, SemanticDomain } from "../mini-lcm";

import type { MenuOption } from "svelte-ux";
import type { Readable } from "svelte/store";

export interface OptionProvider {
  readonly partsOfSpeech: Readable<MenuOption[]>;
}
