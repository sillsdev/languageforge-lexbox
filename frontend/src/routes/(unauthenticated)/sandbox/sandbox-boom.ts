let throwsLeft = 0;
export function armSandboxBoom(): void {
  throwsLeft = 1;
}
export function consumeSandboxBoom(): void {
  if (throwsLeft > 0) {
    throwsLeft -= 1;
    throw new Error('sandbox render error');
  }
}
