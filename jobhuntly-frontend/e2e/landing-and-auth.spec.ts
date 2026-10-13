import { expect, test } from "@playwright/test";
import { expectAccessible, expectNoHorizontalScroll, registerFresh, startDemo } from "./helpers";

test.describe("landing and sign-in", () => {
  test("the landing page offers the demo, discloses AI use, and is accessible", async ({ page }) => {
    await page.goto("/");

    await expect(page.getByRole("heading", { level: 1 })).toContainText("interview");
    await expect(page.getByRole("button", { name: "Try the demo, no signup" })).toBeVisible();
    await expect(page.getByRole("link", { name: "Create an account" })).toBeVisible();
    await expect(page.getByText("text you paste is sent to that provider")).toBeVisible(); // AC-AI-6 (footer disclosure)
    await expectAccessible(page);
    await expectNoHorizontalScroll(page);
  });

  test("one click opens a signed-in sandbox full of sample data", async ({ page }) => {
    await startDemo(page);

    await expect(page.locator("li.card")).toHaveCount(14);
    await expect(page.locator("section.col")).toHaveCount(6);
    await expect(page.getByText(/demo sandbox · expires in/)).toBeVisible();
    await expect(page.getByRole("link", { name: /^Today/ })).toContainText("3");
  });

  test("signed-out visitors are sent to the sign-in page", async ({ page }) => {
    await page.goto("/board");

    await expect(page).toHaveURL(/\/login/);
    await expect(page.getByRole("heading", { name: "Sign in" })).toBeVisible();
    await expectAccessible(page);
  });

  test("register, log out, log in again, and a wrong password is explained", async ({ page }) => {
    const email = await registerFresh(page);
    await expect(page.getByText("Nothing here yet")).toBeVisible(); // a new notebook starts empty

    await page.getByRole("button", { name: "Log out" }).click();
    await expect(page).toHaveURL(/\/$/);
    await page.goto("/board");
    await expect(page).toHaveURL(/\/login/);

    await page.getByLabel("Email").fill(email);
    await page.getByLabel("Password").fill("not-the-password-1");
    await page.getByRole("button", { name: "Sign in" }).click();
    await expect(page.locator(".notice.error")).toContainText(/credentials/i);

    await page.getByLabel("Password").fill("correct-horse-battery");
    await page.getByRole("button", { name: "Sign in" }).click();
    await expect(page).toHaveURL(/\/board$/);
  });

  test("registering enforces the password length before asking the server", async ({ page }) => {
    await page.goto("/register");
    await page.getByLabel("First name").fill("A");
    await page.getByLabel("Last name").fill("B");
    await page.getByLabel("Email").fill("itest-e2e-short@example.test");
    await page.getByLabel("Password").fill("short");
    await page.getByRole("button", { name: "Create account" }).click();

    await expect(page).toHaveURL(/\/register/);
    const valid = await page.getByLabel("Password").evaluate((el: HTMLInputElement) => el.validity.tooShort);
    expect(valid).toBe(true);
  });
});
