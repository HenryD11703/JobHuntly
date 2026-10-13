import { expect, test, type Page } from "@playwright/test";
import { card, expectAccessible, expectNoHorizontalScroll, startDemo } from "./helpers";

async function open(page: Page, company: string) {
  await card(page, company).getByRole("link", { name: new RegExp(company) }).click();
  await expect(page.getByRole("heading", { level: 1, name: company })).toBeVisible();
}

test.describe("application detail", () => {
  test("shows the next interview, its checklist, the saved questions and the timeline", async ({ page }) => {
    await startDemo(page);
    await open(page, "Northwind Labs");

    await expect(page.getByText("Technical round")).toBeVisible();
    await expect(page.getByRole("heading", { name: /^Checklist/ })).toContainText("3 of 5");
    await expect(page.getByText("How would you design rate limiting for a public API?")).toBeVisible(); // saved with the seed: no AI call
    await expect(page.getByText('Story: "Rate limiter for the billing API"')).toBeVisible();
    await expect(page.getByRole("heading", { name: "Timeline" })).toBeVisible();
    await expect(page.getByText("Follow-up sent: Emailed the hiring manager")).toBeVisible();
    await expectAccessible(page);
    await expectNoHorizontalScroll(page);
  });

  test("ticking the checklist persists", async ({ page }) => {
    await startDemo(page);
    await open(page, "Northwind Labs");

    await page.getByLabel("Prepare 2 questions to ask the interviewer").check(); // AC-3.2
    await expect(page.getByRole("heading", { name: /^Checklist/ })).toContainText("4 of 5");
    await page.reload();

    await expect(page.getByRole("heading", { name: /^Checklist/ })).toContainText("4 of 5");
    await expect(page.getByLabel("Prepare 2 questions to ask the interviewer")).toBeChecked();
  });

  test("completing the checklist clears the Prepare reminder", async ({ page }) => {
    await startDemo(page);
    await open(page, "Northwind Labs");

    await page.getByLabel("Prepare 2 questions to ask the interviewer").check();
    await page.getByLabel("Solve one practice problem under a timer").check();
    await expect(page.getByRole("heading", { name: /^Checklist/ })).toContainText("5 of 5");

    await page.getByRole("link", { name: /^Today/ }).click();
    await expect(page.getByRole("heading", { name: /^Prepare/ })).toHaveCount(0);
  });

  test("AI can regenerate the likely questions after the notice, and the new set is saved", async ({ page }) => {
    await startDemo(page);
    await open(page, "Northwind Labs");

    await page.getByRole("button", { name: /^Regenerate/ }).click();
    await page.getByRole("dialog", { name: "Before you use AI" }).getByRole("button", { name: "I understand, continue" }).click();

    await expect(page.getByText("When would you pick PostgreSQL over a document store?")).toBeVisible(); // AC-3.3
    await expect(page.getByText("Suggestions only: you decide what to practise.")).toBeVisible();
    await page.reload();
    await expect(page.getByText("When would you pick PostgreSQL over a document store?")).toBeVisible(); // AC-3.4
  });

  test("an old debrief is shown and can be edited", async ({ page }) => {
    await startDemo(page);
    await open(page, "Northwind Labs");

    await page.getByRole("button", { name: /^HR screen/ }).click();
    await expect(page.getByRole("heading", { name: /^Debrief/ })).toContainText("Saved");
    await expect(page.getByLabel("My notes")).toHaveValue(/Talked salary range early/);
    await expect(page.getByLabel("Went poorly")).toHaveValue("Hesitated on team size");
  });

  test("a missing debrief is written with AI help and then clears the reminder", async ({ page }) => {
    await startDemo(page);
    await open(page, "Cobalt Street");

    await expect(page.getByRole("heading", { name: /^Debrief/ })).toBeVisible();
    await page.getByLabel("My notes").fill("Friendly call. Asked about salary and team size. I stumbled on team size.");
    await page.getByRole("button", { name: /Structure my notes/ }).click();
    await page.getByRole("dialog", { name: "Before you use AI" }).getByRole("button", { name: "I understand, continue" }).click();

    await expect(page.getByText("AI draft.", { exact: false }).first()).toBeVisible(); // AC-4.2
    await expect(page.getByLabel("Questions asked (one per line)")).toHaveValue(/Salary expectations/);
    await page.getByLabel("Follow-ups").fill("Prepare an answer about team size");

    await page.getByRole("button", { name: "Save debrief" }).click(); // AC-4.3
    await expect(page.getByText("Debrief saved.")).toBeVisible();
    await expect(page.getByRole("heading", { name: /^Debrief/ })).toContainText("Saved");

    await page.getByRole("link", { name: /^Today/ }).click();
    await expect(page.getByRole("heading", { name: /^Write a debrief/ })).toHaveCount(0);
  });

  test("an interview can be added and appears with its checklist", async ({ page }) => {
    await startDemo(page);
    await open(page, "Orbitcraft");

    await page.getByRole("button", { name: "+ Add interview" }).click();
    const dialog = page.getByRole("dialog", { name: "Add interview" });
    await dialog.getByLabel("Round").selectOption("Behavioral");
    await dialog.getByRole("button", { name: "Add interview" }).click();

    await expect(page.getByText("Behavioral round")).toBeVisible();
    await expect(page.getByRole("heading", { name: /^Checklist/ })).toContainText("0 of 5");
    await expect(page.getByText("Behavioral interview added")).toBeVisible();
  });

  test("a follow-up can be logged and shows in the timeline", async ({ page }) => {
    await startDemo(page);
    await open(page, "Tessel");

    await page.getByRole("button", { name: "Log a follow-up" }).click();
    await page.getByRole("dialog", { name: "Log a follow-up" }).getByLabel("What did you send? (optional)").fill("Pinged my referrer");
    await page.getByRole("dialog", { name: "Log a follow-up" }).getByRole("button", { name: "Log follow-up" }).click();

    await expect(page.getByText("Follow-up sent: Pinged my referrer")).toBeVisible();
  });

  test("details can be edited and the stage can be changed from the page", async ({ page }) => {
    await startDemo(page);
    await open(page, "Harbor & Pine");

    await page.getByRole("button", { name: "Edit details" }).click();
    const dialog = page.getByRole("dialog", { name: "Edit details" });
    await dialog.getByLabel("Salary range").fill("USD 4,000 / month");
    await dialog.getByRole("button", { name: "Save details" }).click();
    await expect(page.getByText("USD 4,000 / month").first()).toBeVisible();

    await page.getByLabel("Stage").selectOption("Applied");
    await expect(page.getByText("Moved to Applied")).toBeVisible();
  });

  test("an application can be deleted after confirming", async ({ page }) => {
    await startDemo(page);
    await open(page, "Gridline");

    await page.getByRole("button", { name: "Delete this application" }).click();
    await page.getByRole("dialog", { name: "Delete application?" }).getByRole("button", { name: "Delete permanently" }).click();

    await expect(page).toHaveURL(/\/board$/);
    await expect(page.locator("li.card")).toHaveCount(13);
  });

  test("an offer can be added from the application", async ({ page }) => {
    await startDemo(page);
    await open(page, "Silverline");

    await page.getByRole("button", { name: "+ Add an offer" }).click();
    const dialog = page.getByRole("dialog", { name: "Add an offer" });
    await dialog.getByLabel("Amount (gross)").fill("3900");
    await dialog.getByRole("button", { name: "Add offer" }).click();

    await expect(page.getByText("Offer saved.")).toBeVisible();
    await page.getByRole("link", { name: "Compare offers →" }).click();
    await expect(page.getByRole("row", { name: /Silverline/ })).toHaveCount(2);
  });

  test("an unknown application shows a clear error", async ({ page }) => {
    await startDemo(page);

    await page.goto("/applications/00000000-0000-0000-0000-000000000000");

    await expect(page.locator(".notice.error")).toContainText(/not found/i);
  });
});
