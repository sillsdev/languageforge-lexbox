import {Context} from 'runed';
import {type crossfade} from 'svelte/transition';

export const transitionContext = new Context<ReturnType<typeof crossfade>>("home-transitions");
