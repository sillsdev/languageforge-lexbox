import type { AnyZodObject } from 'zod';
import type { SuperForm } from 'sveltekit-superforms/client';
import type { ZodValidation } from 'sveltekit-superforms/index';

export type AnySuperForm = SuperForm<ZodValidation<AnyZodObject>>;
