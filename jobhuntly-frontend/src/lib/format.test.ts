import { describe, expect, it } from "vitest";
import {
  daysAgo, daysBetween, fixed2, fromLocalInput, linesToList, listToLines, money, number, roundLabel,
  sourceLabel, stageLabel, statusLabel, timeLeft, timeUntil, toLocalInput,
} from "./format";

const NOW = new Date("2026-10-13T12:00:00Z");

describe("labels", () => {
  it("turns enum names into readable words", () => {
    expect(statusLabel("TechnicalTest")).toBe("Technical test");
    expect(roundLabel("HrScreen")).toBe("HR screen");
    expect(roundLabel("TakeHome")).toBe("Take-home");
    expect(sourceLabel("CompanySite")).toBe("Company site");
    expect(stageLabel("Interviewing")).toBe("Interviewing");
  });

  it("falls back to the raw value for something unknown instead of crashing", () => {
    expect(sourceLabel("Billboard")).toBe("Billboard");
    expect(stageLabel("Mystery")).toBe("Mystery");
  });
});

describe("relative time", () => {
  it("counts whole days and never goes negative", () => {
    expect(daysBetween("2026-10-04T12:00:00Z", NOW)).toBe(9);
    expect(daysBetween("2026-10-04T13:00:00Z", NOW)).toBe(8); // 8 days 23 h
    expect(daysBetween("2026-10-20T12:00:00Z", NOW)).toBe(0); // in the future
  });

  it("describes the past", () => {
    expect(daysAgo("2026-10-13T08:00:00Z", NOW)).toBe("today");
    expect(daysAgo("2026-10-12T08:00:00Z", NOW)).toBe("1 day ago");
    expect(daysAgo("2026-10-04T12:00:00Z", NOW)).toBe("9 days ago");
  });

  it("describes the future", () => {
    expect(timeUntil("2026-10-13T12:30:00Z", NOW)).toBe("in under an hour");
    expect(timeUntil("2026-10-13T13:00:00Z", NOW)).toBe("in 1 hour");
    expect(timeUntil("2026-10-14T18:00:00Z", NOW)).toBe("in 30 hours");
    expect(timeUntil("2026-10-18T12:00:00Z", NOW)).toBe("in 5 days");
    expect(timeUntil("2026-10-13T11:00:00Z", NOW)).toBe("now");
  });

  it("shows how long a demo sandbox has left", () => {
    expect(timeLeft("2026-10-14T12:00:00Z", NOW)).toBe("24 h");
    expect(timeLeft("2026-10-13T12:45:00Z", NOW)).toBe("45 min");
    expect(timeLeft("2026-10-13T12:00:10Z", NOW)).toBe("1 min");
    expect(timeLeft("2026-10-13T11:00:00Z", NOW)).toBe("expired");
  });
});

describe("numbers and money", () => {
  it("formats amounts without symbols so currencies are never confused", () => {
    expect(money(3400, "USD")).toBe("USD 3,400");
    expect(money(38400.5, "EUR")).toBe("EUR 38,400.5");
    expect(money(24000, "GTQ")).toBe("GTQ 24,000");
  });

  it("keeps two decimals in comparison tables", () => {
    expect(fixed2(3400)).toBe("3,400.00");
    expect(fixed2(3576.96)).toBe("3,576.96");
    expect(fixed2(25976)).toBe("25,976.00");
  });

  it("limits decimals", () => {
    expect(number(33.333333, 1)).toBe("33.3");
    expect(number(7)).toBe("7");
  });
});

describe("dates for forms", () => {
  it("round-trips a datetime-local value through an ISO instant", () => {
    const local = "2026-10-15T10:00";
    expect(toLocalInput(fromLocalInput(local))).toBe(local);
    expect(fromLocalInput(local)).toMatch(/^2026-10-15T\d\d:00:00\.000Z$/);
  });
});

describe("line lists", () => {
  it("turns text into trimmed non-empty items and back", () => {
    expect(linesToList("  Salary?  \n\n Team size \n   ")).toEqual(["Salary?", "Team size"]);
    expect(listToLines(["a", "b"])).toBe("a\nb");
    expect(linesToList("")).toEqual([]);
  });
});
