import { expect, test } from '@playwright/test';

test('component lab renders the disabled schedule stream button', async ({ page }) => {
  await page.goto('/component-lab');

  await expect(page.getByRole('heading', { name: 'Component Lab' })).toBeVisible();

  // The lab shows one component at a time; select Button from the list first.
  await page.getByRole('button', { name: 'Button', exact: true }).click();

  await expect(page.getByRole('heading', { name: 'Button' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Schedule stream' })).toBeDisabled();
});
