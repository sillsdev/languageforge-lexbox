export function randomFieldId(): string {
	return crypto.randomUUID().split('-').at(-1) as string;
}
