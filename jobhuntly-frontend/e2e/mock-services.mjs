// Local stand-ins for the two third-party services, so end-to-end tests never touch the internet.
//  - Groq (OpenAI-compatible chat completions): answers by looking at which feature is asking.
//  - Exchange rates (open.er-api.com format).
import http from "node:http";

const PORT = 5099;
const EXPECTED_KEY = "e2e-mock-key";

const answers = {
  extract: {
    companyName: "Northwind Labs",
    positionTitle: "Backend Engineer",
    salaryRange: "USD 3,000 to 3,600 / month",
    location: "Remote (LATAM)",
    isRemote: true,
    requirements: ["C#", "PostgreSQL", "Kafka"],
  },
  debrief: {
    questionsAsked: ["Salary expectations", "Team size"],
    wentWell: ["Salary accepted"],
    wentPoorly: ["Hesitated on team size"],
    followUps: ["Prepare an answer about team size"],
  },
  prep: {
    questions: [
      { question: "How would you design rate limiting for a public API?" },
      { question: "Tell me about a production bug you debugged." },
      { question: "When would you pick PostgreSQL over a document store?" },
    ],
  },
};

function pick(system) {
  if (/extract structured data/i.test(system)) return answers.extract;
  if (/structured notes/i.test(system)) return answers.debrief;
  return answers.prep;
}

const server = http.createServer((req, res) => {
  if (req.method === "GET" && req.url === "/health") {
    res.writeHead(200, { "content-type": "application/json" }).end('{"status":"ok"}');
    return;
  }

  if (req.method === "GET" && req.url?.startsWith("/v6/latest/USD")) {
    res.writeHead(200, { "content-type": "application/json" }).end(
      JSON.stringify({
        result: "success",
        time_last_update_unix: Math.floor(Date.now() / 1000),
        rates: { USD: 1, GTQ: 7.7, EUR: 0.9 },
      }),
    );
    return;
  }

  if (req.method === "POST" && req.url === "/openai/v1/chat/completions") {
    let body = "";
    req.on("data", (chunk) => (body += chunk));
    req.on("end", () => {
      if (req.headers.authorization !== `Bearer ${EXPECTED_KEY}`) {
        res.writeHead(401, { "content-type": "application/json" }).end('{"error":{"message":"bad key"}}');
        return;
      }
      const payload = JSON.parse(body);
      const system = payload.messages.find((m) => m.role === "system")?.content ?? "";
      res.writeHead(200, { "content-type": "application/json" }).end(
        JSON.stringify({ choices: [{ message: { role: "assistant", content: JSON.stringify(pick(system)) } }] }),
      );
    });
    return;
  }

  res.writeHead(404).end();
});

server.listen(PORT, () => console.log(`mock services on ${PORT}`));
