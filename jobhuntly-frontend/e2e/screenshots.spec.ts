import { test } from "@playwright/test";
import path from "node:path";
import { card, startDemo } from "./helpers";

// Not part of the normal run: `SHOTS=1 npx playwright test e2e/screenshots.spec.ts --project=desktop`
// regenerates the README images in docs/screenshots from the real app and the demo sandbox.
const shots = path.resolve(__dirname, "../../docs/screenshots");

test.describe("screenshots", () => {
  test.skip(process.env.SHOTS !== "1", "set SHOTS=1 to regenerate the README screenshots");

  test("capture the main screens", async ({ page }, testInfo) => {
    const prefix = testInfo.project.name === "mobile" ? "mobile-" : "";
    await startDemo(page);
    await page.screenshot({ path: path.join(shots, `${prefix}board.png`), fullPage: true });

    await card(page, "Northwind Labs").getByRole("link", { name: /Northwind Labs/ }).click();
    await page.getByRole("heading", { level: 1, name: "Northwind Labs" }).waitFor();
    await page.locator(".timeline li").first().waitFor();
    await page.screenshot({ path: path.join(shots, `${prefix}detail.png`), fullPage: true });

    await page.getByRole("link", { name: /^Today/ }).click();
    await page.getByRole("heading", { name: /^Prepare/ }).waitFor();
    await page.screenshot({ path: path.join(shots, `${prefix}today.png`), fullPage: true });

    await page.getByRole("link", { name: "Insights" }).click();
    await page.getByRole("heading", { name: "Funnel" }).waitFor();
    await page.screenshot({ path: path.join(shots, `${prefix}insights.png`), fullPage: true });

    await page.getByRole("link", { name: "Offers" }).click();
    await page.getByRole("heading", { name: /^Side by side/ }).waitFor();
    await page.screenshot({ path: path.join(shots, `${prefix}offers.png`), fullPage: true });
  });
});
