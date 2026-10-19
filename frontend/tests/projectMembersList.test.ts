import {addUserToProject, loginAs} from './utils/authHelpers';

import {ProjectPage} from './pages/projectPage';
import {expect} from '@playwright/test';
import {test} from './fixtures';

// A member badge lays a member's name out beside a role badge and a ⋮ menu. Once more than five
// members are shown the list becomes a grid of fixed-width tracks, so a badge can no longer grow to
// fit a long name — the name has to truncate instead, or it shoves the role badge and the ⋮ menu out
// past the badge's right edge and on top of the neighbouring badge.
const LONG_NAME = 'John Jacob Jingleheimer Schmidt';
// MembersList switches from a flex row to a grid once *more* than this many members are shown.
const MEMBERS_SHOWN_BEFORE_GRID = 5;

test('a long member name does not push the member menu out of its badge', async ({page, tempProject, uniqueTestId, guestUserFactory}) => {
  await loginAs(page.request, 'admin');

  const members = [await guestUserFactory(LONG_NAME, `jjjs-${uniqueTestId}`)];
  for (let i = 0; i < MEMBERS_SHOWN_BEFORE_GRID; i++) {
    members.push(await guestUserFactory(`Member ${i}`, `member-${i}-${uniqueTestId}`));
  }
  for (const member of members) {
    await addUserToProject(page.request, member.id, tempProject.id, 'EDITOR');
  }

  await new ProjectPage(page, tempProject.name, tempProject.code).goto();
  await page.getByRole('button', {name: 'Show all...'}).click();

  // The grid layout is the only one where a badge can't grow to fit its name, so assert we reached
  // it rather than letting the test quietly pass by measuring the flex layout instead.
  const memberGrid = page.locator('.badge-list.grid').filter({hasText: LONG_NAME});
  await expect(memberGrid, `all ${members.length} members should be shown, in a grid`).toBeVisible();

  const memberBadge = memberGrid.locator('.dropdown-container', {hasText: LONG_NAME});
  const memberMenu = memberBadge.locator('.btn-circle');
  await expect(memberMenu).toBeVisible();

  const badgeBox = (await memberBadge.boundingBox())!;
  const menuBox = (await memberMenu.boundingBox())!;
  expect(
    menuBox.x + menuBox.width,
    'the ⋮ menu must stay inside its member badge, not be pushed off to the right by the long name',
  ).toBeLessThanOrEqual(badgeBox.x + badgeBox.width);

  // Guards the assertion above against becoming meaningless: if the name were to fit in the badge
  // there would be nothing for a missing truncation to push out of place.
  const nameIsTruncated = await memberBadge
    .locator(`[title="${LONG_NAME}"]`)
    .evaluate((name) => name.scrollWidth > name.clientWidth);
  expect(nameIsTruncated, `"${LONG_NAME}" must be too wide for its badge, or this test proves nothing`).toBe(true);
});
