import AxeBuilder from "@axe-core/playwright";
import { expect, type Page } from "@playwright/test";

/** Opens the landing page and starts a private demo sandbox: 14 sample applications. */
export async function startDemo(page: Page) {
  await page.goto("/");
  await page.getByRole("button", { name: "Try the demo, no signup" }).click();
  // Creating a sandbox inserts a few hundred rows; the very first one also pays for server start-up.
  await expect(page).toHaveURL(/\/board$/, { timeout: 60_000 });
  await expect(page.getByRole("heading", { name: "Pipeline" })).toBeVisible();
  await expect(page.locator(".card").first()).toBeVisible();
}

let counter = 0;

/** Registers a brand new account (empty notebook). Emails start with "itest-" so test cleanup removes them. */
export async function registerFresh(page: Page) {
  const email = `itest-e2e-${Date.now()}-${counter++}@example.test`;
  await page.goto("/register");
  await page.getByLabel("First name").fill("Test");
  await page.getByLabel("Last name").fill("Person");
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Password").fill("correct-horse-battery");
  await page.getByRole("button", { name: "Create account" }).click();
  await expect(page).toHaveURL(/\/board$/);
  return email;
}

/** axe: no serious or critical accessibility violations (AC-NF-1). */
export async function expectAccessible(page: Page) {
  const results = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"]).analyze();
  const blocking = results.violations.filter((v) => v.impact === "serious" || v.impact === "critical");
  expect(
    blocking.map((v) => `${v.id}: ${v.help} (${v.nodes.length} nodes, first: ${v.nodes[0]?.target?.join(" ")})`),
    "serious or critical accessibility violations",
  ).toEqual([]);
}

/** No horizontal page scroll (AC-2.3). */
export async function expectNoHorizontalScroll(page: Page) {
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
  expect(overflow, "horizontal overflow in px").toBeLessThanOrEqual(0);
}

/** The column section whose heading starts with the given stage name. */
export function column(page: Page, stage: string) {
  return page.locator("section.col").filter({ has: page.getByRole("heading", { name: new RegExp(`^${stage}`) }) });
}

export function card(page: Page, company: string) {
  return page.locator("li.card").filter({ hasText: company });
}
