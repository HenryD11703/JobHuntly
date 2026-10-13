import { expect, test } from "@playwright/test";
import { card, column, expectAccessible, expectNoHorizontalScroll, startDemo } from "./helpers";

test.describe("board", () => {
  test("the board is accessible and fits the screen", async ({ page }) => {
    await startDemo(page);

    await expectAccessible(page);
    await expectNoHorizontalScroll(page); // AC-2.3 at 375 px on the mobile project
  });

  test("a card moves to another stage with the stage menu and the move survives a reload", async ({ page }) => {
    await startDemo(page);
    await expect(column(page, "Backlog").locator("li.card")).toHaveCount(2);

    await card(page, "Harbor & Pine").getByLabel(/Move Harbor & Pine/).selectOption("Applied");

    await expect(column(page, "Backlog").locator("li.card")).toHaveCount(1);
    await expect(column(page, "Applied").locator("li.card").filter({ hasText: "Harbor & Pine" })).toBeVisible();
    await page.reload();
    await expect(column(page, "Applied").locator("li.card").filter({ hasText: "Harbor & Pine" })).toBeVisible(); // AC-2.1
  });

  test("a card can be moved with the keyboard alone", async ({ page }) => {
    await startDemo(page);
    const select = card(page, "Quillmark").getByLabel(/Move Quillmark/);

    await select.focus();
    await expect(select).toBeFocused();
    await page.keyboard.press("ArrowDown"); // "Stage: Backlog" -> "Move to Applied"
    await page.keyboard.press("ArrowDown");

    await expect(column(page, "Applied").locator("li.card").filter({ hasText: "Quillmark" })).toBeVisible(); // AC-2.2
  });

  test("a card can be dragged to another column", async ({ page, isMobile }) => {
    test.skip(isMobile, "drag and drop is a mouse feature; touch users use the stage menu");
    await startDemo(page);

    await card(page, "Mapleseed").dragTo(column(page, "Interviewing"));

    await expect(column(page, "Interviewing").locator("li.card").filter({ hasText: "Mapleseed" })).toBeVisible();
  });

  test("moving to Rejected asks for an optional reason and shows it on the card", async ({ page }) => {
    await startDemo(page);

    await card(page, "Fernwood").getByLabel(/Move Fernwood/).selectOption("Rejected");
    const dialog = page.getByRole("dialog", { name: "Why was it rejected?" });
    await expect(dialog).toBeVisible();
    await dialog.getByLabel("Reason (optional)").fill("Role closed");
    await dialog.getByRole("button", { name: "Move to Rejected" }).click();

    await expect(column(page, "Rejected").locator("li.card").filter({ hasText: "Fernwood" })).toContainText("ROLE CLOSED");
  });

  test("the reason is optional: cancelling leaves the card where it was", async ({ page }) => {
    await startDemo(page);

    await card(page, "Orbitcraft").getByLabel(/Move Orbitcraft/).selectOption("Rejected");
    await page.getByRole("dialog").getByRole("button", { name: "Cancel" }).click();

    await expect(column(page, "Applied").locator("li.card").filter({ hasText: "Orbitcraft" })).toBeVisible();
  });

  test("cards that need action are marked, with the reason in words", async ({ page }) => {
    await startDemo(page);

    await expect(card(page, "Tessel")).toContainText("Follow up");
    await expect(card(page, "Northwind Labs")).toContainText("Prepare");
    await expect(card(page, "Cobalt Street")).toContainText("Write debrief");
    await expect(page.locator("li.card.needs")).toHaveCount(3);
  });

  test("a new application can be created by hand", async ({ page }) => {
    await startDemo(page);

    await page.getByRole("button", { name: "+ New application" }).click();
    const dialog = page.getByRole("dialog", { name: "New application" });
    await dialog.getByLabel("Company", { exact: true }).fill("Moonrise Studio");
    await dialog.getByLabel("Position", { exact: true }).fill("Staff Engineer");
    await dialog.getByRole("combobox", { name: /^Source/ }).selectOption("Referral");
    await dialog.getByRole("button", { name: "Create application" }).click();

    await expect(column(page, "Backlog").locator("li.card").filter({ hasText: "Moonrise Studio" })).toBeVisible(); // AC-1.1
    await expect(page.locator("li.card")).toHaveCount(15);
  });

  test("company and position are required", async ({ page }) => {
    await startDemo(page);

    await page.getByRole("button", { name: "+ New application" }).click();
    await page.getByRole("dialog").getByRole("button", { name: "Create application" }).click();

    await expect(page.getByRole("dialog", { name: "New application" })).toBeVisible();
    await expect(page.locator("li.card")).toHaveCount(14);
  });

  test("a pasted job post fills a draft after a one-time AI notice, and nothing is saved until Create", async ({ page }) => {
    await startDemo(page);
    await page.getByRole("button", { name: "+ New application" }).click();
    const dialog = page.getByRole("dialog", { name: "New application" });
    await dialog.getByLabel("Job post text (optional)").fill(
      "Northwind Labs is hiring a Backend Engineer. Remote within LATAM. C#, PostgreSQL, Kafka. USD 3,000 to 3,600 per month.",
    );

    await dialog.getByRole("button", { name: /Fill the form from this text/ }).click();

    // AC-AI-6: the disclosure appears before the first use.
    const notice = page.getByRole("dialog", { name: "Before you use AI" });
    await expect(notice).toContainText("third-party AI provider");
    await notice.getByRole("button", { name: "I understand, continue" }).click();

    await expect(dialog.getByLabel("Company", { exact: true })).toHaveValue("Northwind Labs"); // AC-1.3
    await expect(dialog.getByLabel("Position", { exact: true })).toHaveValue("Backend Engineer");
    await expect(dialog.getByText("AI draft.")).toBeVisible();
    await expect(page.locator("li.card")).toHaveCount(14); // still nothing saved

    await dialog.getByRole("button", { name: "Create application" }).click();
    await expect(page.locator("li.card")).toHaveCount(15);
  });

  test("the AI notice is only shown once", async ({ page }) => {
    await startDemo(page);
    const text = "Acme is hiring a Frontend Developer in Guatemala City. React and TypeScript. On site.";

    await page.getByRole("button", { name: "+ New application" }).click();
    let dialog = page.getByRole("dialog", { name: "New application" });
    await dialog.getByLabel("Job post text (optional)").fill(text);
    await dialog.getByRole("button", { name: /Fill the form from this text/ }).click();
    await page.getByRole("dialog", { name: "Before you use AI" }).getByRole("button", { name: "I understand, continue" }).click();
    await expect(dialog.getByText("AI draft.")).toBeVisible();
    await dialog.getByRole("button", { name: "Cancel" }).click();

    await page.getByRole("button", { name: "+ New application" }).click();
    dialog = page.getByRole("dialog", { name: "New application" });
    await dialog.getByLabel("Job post text (optional)").fill(text);
    await dialog.getByRole("button", { name: /Fill the form from this text/ }).click();

    await expect(page.getByRole("dialog", { name: "Before you use AI" })).toHaveCount(0);
    await expect(dialog.getByText("AI draft.")).toBeVisible();
  });
});
