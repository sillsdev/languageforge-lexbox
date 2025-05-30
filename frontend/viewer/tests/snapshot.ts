import { argosScreenshot } from "@argos-ci/playwright";
export function assertSnapshot(...args: Parameters<typeof argosScreenshot>): Promise<void> {
  args[2] = {
    viewports: [
      { height: 720, width: 1280 },
      'iphone-x'
    ],
    ...args[2]
  };
  return argosScreenshot(...args);
}
