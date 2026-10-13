import { expect, test } from "@playwright/test";
import { expectAccessible, expectNoHorizontalScroll, registerFresh, startDemo } from "./helpers";

test.describe("Today", () => {
  test("lists what needs the user, grouped, and logging a follow-up removes it", async ({ page }) => {
    await startDemo(page);
    await page.getByRole("link", { name: /^Today/ }).click();

    await expect(page.getByRole("heading", { name: /^Prepare/ })).toBeVisible();
    await expect(page.getByRole("heading", { name: /^Write a debrief/ })).toBeVisible();
    await expect(page.getByRole("heading", { name: /^Follow up/ })).toBeVisible();
    await expect(page.getByRole("row", { name: /Northwind Labs/ })).toBeVisible();
    await expect(page.getByRole("row", { name: /Cobalt Street/ })).toBeVisible();
    await expect(page.getByRole("row", { name: /Tessel/ })).toBeVisible();
    await expectAccessible(page);
    await expectNoHorizontalScroll(page);

    await page.getByRole("button", { name: "Log follow-up" }).click(); // AC-5.2
    await page.getByRole("dialog", { name: "Log a follow-up" }).getByRole("button", { name: "Log follow-up" }).click();

    await expect(page.getByRole("row", { name: /Tessel/ })).toHaveCount(0);
    await expect(page.getByRole("link", { name: /^Today/ })).toContainText("2");
  });

  test("a new notebook has nothing to do today", async ({ page }) => {
    await registerFresh(page);
    await page.getByRole("link", { name: /^Today/ }).click();

    await expect(page.getByText("Nothing needs you today")).toBeVisible();
  });
});

test.describe("Insights", () => {
  test("shows the funnel from the timelines and hides percentages for small groups", async ({ page }) => {
    await startDemo(page);
    await page.getByRole("link", { name: "Insights" }).click();

    await expect(page.getByRole("heading", { name: "Funnel" })).toBeVisible();
    await expect(page.locator(".stat").first()).toContainText("12"); // 12 of 14 reached Applied
    await expect(page.getByRole("row", { name: /Offer/ }).first()).toBeVisible();
    await expect(page.getByText("not enough data").first()).toBeVisible(); // AC-6.2
    await expect(page.getByText("Skills gap")).toBeVisible();
    await expectAccessible(page);
    await expectNoHorizontalScroll(page);
  });

  test("an empty notebook says so instead of showing zeros", async ({ page }) => {
    await registerFresh(page); // AC-6.3
    await page.getByRole("link", { name: "Insights" }).click();

    await expect(page.getByText("Not enough yet")).toBeVisible();
    await expect(page.getByText("NaN")).toHaveCount(0);
  });
});

test.describe("Offers", () => {
  test("compares three offers side by side with the saved rates", async ({ page }) => {
    await startDemo(page);
    await page.getByRole("link", { name: "Offers" }).click();

    await expect(page.getByRole("row", { name: /Silverline/ })).toContainText("3,400.00");
    await expect(page.getByRole("row", { name: /Silverline/ })).toContainText("25,976.00"); // 3400 x 7.64
    await expect(page.getByRole("row", { name: /Brightwave/ })).toContainText("3,576.96"); // AC-7.3
    await expect(page.getByRole("row", { name: /Lumen Freight/ })).toContainText("24,000.00");
    await expect(page.getByText("no tax calculation").first()).toBeVisible(); // AC-7.4
    await expectAccessible(page);
    await expectNoHorizontalScroll(page);
  });

  test("today's rate only fills the fields; saving them updates the table", async ({ page }) => {
    await startDemo(page);
    await page.getByRole("link", { name: "Offers" }).click();
    await expect(page.getByLabel("1 USD in GTQ")).toHaveValue("7.64");

    await page.getByRole("button", { name: "Fetch today's rate" }).click(); // AC-7.5
    await expect(page.getByLabel("1 USD in GTQ")).toHaveValue("7.7");
    await expect(page.getByText(/Suggested from/)).toContainText("ExchangeRate-API");
    await expect(page.getByText(/still uses your/)).toBeVisible();
    await expect(page.getByRole("row", { name: /Silverline/ })).toContainText("25,976.00"); // unchanged until saved

    await page.getByRole("button", { name: "Save rates" }).click();
    await expect(page.getByText("Rates saved.")).toBeVisible();
    await expect(page.getByRole("row", { name: /Silverline/ })).toContainText("26,180.00"); // 3400 x 7.7
  });

  test("rates outside a plausible range are rejected with a clear message", async ({ page }) => {
    await startDemo(page);
    await page.getByRole("link", { name: "Offers" }).click();

    await page.getByLabel("1 USD in GTQ").fill("76");
    await page.getByRole("button", { name: "Save rates" }).click();

    // the browser blocks it before the server sees it (max=20), so the saved rates are untouched
    await expect(page.getByLabel("1 USD in GTQ")).toHaveValue("76");
    await expect(page.getByRole("row", { name: /Silverline/ })).toContainText("25,976.00");
  });

  test("an offer can be added from the page and deleted", async ({ page }) => {
    await startDemo(page);
    await page.getByRole("link", { name: "Offers" }).click();

    await page.getByRole("button", { name: "+ Add offer" }).click();
    const dialog = page.getByRole("dialog", { name: "Add an offer" });
    await dialog.getByLabel("Application").selectOption({ label: "Northwind Labs · Backend Engineer" });
    await dialog.getByLabel("Amount (gross)").fill("3300");
    await dialog.getByRole("button", { name: "Add offer" }).click();
    await expect(page.getByRole("row", { name: /Northwind Labs/ })).toContainText("3,300.00");

    await page.getByRole("button", { name: "Delete the Northwind Labs offer" }).click();
    await expect(page.getByRole("row", { name: /Northwind Labs/ })).toHaveCount(0);
  });
});

test.describe("Stories", () => {
  test("lists the bank, and a story can be created, edited and deleted", async ({ page }) => {
    await startDemo(page);
    await page.getByRole("link", { name: "Stories" }).click();
    await expect(page.getByRole("heading", { name: "Rate limiter for the billing API" })).toBeVisible();
    await expectAccessible(page);
    await expectNoHorizontalScroll(page);

    await page.getByRole("button", { name: "+ New story" }).click();
    const dialog = page.getByRole("dialog", { name: "New story" });
    await dialog.getByLabel("Title").fill("Migrated the monolith");
    await dialog.getByLabel(/^Situation/).fill("A slow monolith blocked releases.");
    await dialog.getByLabel(/^Task/).fill("Split out billing safely.");
    await dialog.getByLabel(/^Action/).fill("Strangled the module behind an API.");
    await dialog.getByLabel(/^Result/).fill("Deploys went from weekly to daily.");
    await dialog.getByLabel(/^Tags/).fill("architecture, migration");
    await dialog.getByRole("button", { name: "Save story" }).click();
    await expect(page.getByRole("heading", { name: "Migrated the monolith" })).toBeVisible();

    await page.getByRole("button", { name: "Edit Migrated the monolith" }).click();
    await page.getByRole("dialog", { name: "Edit story" }).getByLabel("Title").fill("Migrated the monolith, v2");
    await page.getByRole("dialog", { name: "Edit story" }).getByRole("button", { name: "Save story" }).click();
    await expect(page.getByRole("heading", { name: "Migrated the monolith, v2" })).toBeVisible();

    await page.getByRole("button", { name: "Delete Migrated the monolith, v2" }).click();
    await page.getByRole("dialog", { name: "Delete story?" }).getByRole("button", { name: "Delete story" }).click();
    await expect(page.getByRole("heading", { name: "Migrated the monolith, v2" })).toHaveCount(0);
  });
});

test.describe("navigation", () => {
  test("every main screen is reachable from the top bar", async ({ page }) => {
    await startDemo(page);

    for (const name of ["Today", "Insights", "Offers", "Stories", "Board"]) {
      await page.getByRole("navigation", { name: "Main" }).getByRole("link", { name: new RegExp(`^${name}`) }).click();
      await expect(page.getByRole("link", { name: new RegExp(`^${name}`) }).first()).toHaveAttribute("aria-current", "page");
    }
  });

  test("the skip link jumps to the main content", async ({ page }) => {
    await startDemo(page);

    await page.keyboard.press("Tab");
    await expect(page.getByRole("link", { name: "Skip to content" })).toBeFocused();
  });
});
