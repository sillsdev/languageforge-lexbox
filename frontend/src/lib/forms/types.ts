import type { AnyZodObject } from 'zod';
import type { SuperForm } from 'sveltekit-superforms/client';
import type { ZodValidation } from 'sveltekit-superforms';

export type AnySuperForm = SuperForm<ZodValidation<AnyZodObject>>;
