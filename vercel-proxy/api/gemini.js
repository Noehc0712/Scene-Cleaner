export default async function handler(req, res) {
  // Unity WebGL에서 다른 도메인의 Vercel API를 호출할 수 있도록 허용
  res.setHeader("Access-Control-Allow-Origin", "*");
  res.setHeader("Access-Control-Allow-Methods", "POST, OPTIONS");
  res.setHeader("Access-Control-Allow-Headers", "Content-Type");

  // 브라우저가 실제 POST 요청 전에 보내는 사전 확인 요청
  if (req.method === "OPTIONS") {
    return res.status(204).end();
  }

  // Gemini 요청은 POST만 허용
  if (req.method !== "POST") {
    return res.status(405).json({
      error: "Method Not Allowed"
    });
  }

  // Vercel 환경변수에서 Gemini API Key 읽기
  const apiKey = process.env.GEMINI_API_KEY;

  if (!apiKey) {
    return res.status(500).json({
      error: "GEMINI_API_KEY is not configured."
    });
  }

  try {
    // Unity가 보낸 Gemini 요청 내용을 그대로 전달
    const requestBody =
      typeof req.body === "string"
        ? req.body
        : JSON.stringify(req.body);

    const geminiResponse = await fetch(
      "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.6-flash:generateContent",
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "x-goog-api-key": apiKey
        },
        body: requestBody
      }
    );

    // Gemini 응답을 Unity에 그대로 반환
    const responseText = await geminiResponse.text();

    res.status(geminiResponse.status);
    res.setHeader(
      "Content-Type",
      geminiResponse.headers.get("content-type") || "application/json"
    );

    return res.send(responseText);
  } catch (error) {
    console.error("Gemini proxy error:", error);

    return res.status(500).json({
      error: "Gemini proxy request failed.",
      message: error.message
    });
  }
}