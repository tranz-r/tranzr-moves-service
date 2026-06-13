# Frontend guide: `QuoteV2` journey (backend source of truth, resume, navigation, concurrency)

Use **`/api/v2/quote/...`**, **`credentials: "include"`** (guest cookie), and treat **`QuoteJourneyResponse`** (`journey` + `quote`) as the only authoritative state after each successful read or write.

---

## 1. Bootstrap

1. Call **`POST /api/v2/quote/ensure`** so the guest cookie exists.
2. Call **`POST /api/v2/quote/init`** with `{ quoteType }` to get the first **`QuoteJourneyResponse`**.
3. Persist in client state:
   - **`quote.id`**
   - **`quote.version`** (row version; same meaning as **`ETag`** on responses)
   - Full **`journey`** + **`quote`** snapshot

**Init response:** body includes `quote.version`; response **`ETag`** matches that value—store either, keep them in sync after each mutation.

---

## 2. Backend as source of truth

- After **every** successful **`GET .../journey-state`** or **PATCH**, **replace** local journey/quote state with the response body (do not merge your own “completed steps” logic on top of the server).
- Use **`journey.steps[]`** for UI:
  - **`complete`** — user may open and edit (subject to your product rules).
  - **`current`** — active step.
  - **`locked`** — do not treat as editable; redirect to **`journey.resumeUrl`** (or equivalent route derived from **`resumeStepKey`** / **`steps`**).
- Do **not** reimplement completion rules in the client; trust **`journey`** + **`quote`** fields.

---

## 3. Hydration & back/forward navigation

- On **reload** or **deep link**, call **`GET /api/v2/quote/{quoteId}/journey-state`**.
- Send **`If-None-Match`** with the last **`ETag`** (or string form of **`quote.version`**) when you have one:
  - **304** — body empty; keep existing in-memory state (version unchanged).
  - **200** — replace state; update **`quote.version`** / **`ETag`**.
- When the user **goes back** to a completed step, **prefill forms from `quote`** (addresses, inventory, schedule, etc.). Do not assume “empty” for completed steps.
- Route guards: if the user lands on a **`locked`** step, send them to the backend-indicated resume route (**`journey.resumeUrl`** / **`resumeStepKey`**).

---

## 4. Optimistic concurrency (required for all PATCHes)

**Rule:** every **PATCH** must send the version the UI last saw for **that** quote row.

- **Header (required):**  
  `If-Match: <quote.version>`  
  Use the **numeric** string from **`quote.version`** (same as **`ETag`** on the last **init** / **PATCH** / **GET journey-state** response).

**Responses:**

| Situation | Typical HTTP | What to do |
|-----------|--------------|------------|
| Missing / bad `If-Match` | **400** (`Quote.IfMatch.Required`) | Prompt dev/user; always send `If-Match` on PATCH. |
| Stale version (someone else updated, or you’re behind) | **412** (`Quote.ConcurrencyConflict`) | **`GET .../journey-state`**, replace state, rehydrate forms, ask user to retry or auto-retry **once** with the new **`quote.version`**. |
| Success | **200** | Replace state from body; store new **`quote.version`** and next **`If-Match`**. |

**ETag:** on **init**, **PATCH**, and **GET journey-state**, align client-held version with **`ETag`** and **`quote.version`** after a successful **200**.

---

## 5. Save-on-next (PATCH only when leaving the step)

1. Validate the step **locally**.
2. On **Next** (only when valid), call the **PATCH** for that step with:
   - correct body for that endpoint, and  
   - **`If-Match: quote.version`**.
3. Keep the user on the step until the request finishes (**loading** / disable double submit).
4. On **200**: update full **`QuoteJourneyResponse`**, then **navigate** to the next screen (from updated **`journey`**, not a hardcoded client-only “next”).
5. On **412**: refresh journey (**GET journey-state**), show a short “quote was updated” message, merge server state into UI, **do not** navigate away until the user confirms or retries.
6. Optional autosave **must not** replace this contract unless product explicitly wants background saves with the same **`If-Match`** / conflict handling.

**Endpoint map (v2):**

| Step area | Method |
|-----------|--------|
| Collection / delivery addresses | `PATCH .../collection-delivery-addresses` |
| Inventory | `PATCH .../inventory` |
| Move date / time | `PATCH .../move-date-time` |
| Email / phone | `PATCH .../customer-email-phone` |
| Pricing | `PATCH .../pricing` |
| Customer / billing | `PATCH .../customer-info` |
| Summary | `PATCH .../quote-summary` |

---

## 6. Resume

- **Same device / session:** use stored **`quoteId`** + **`GET .../journey-state`** to hydrate and then follow **`journey`**.
- **Token link (email):**
  1. Call **`POST /api/v2/quote/ensure`** so the guest cookie exists.
  2. Call **`POST /api/v2/quote/resume`** with `{ token }` (from the link query string).
  3. On **200**, call **`GET /api/v2/quote/{quoteId}/journey-state`** to hydrate the full quote.
  4. Navigate using **`journey.resumeUrl`** / **`resumeStepKey`**; strip `token` from the URL.

The API **rebinds** the quote to the current `tranzr_guest` cookie when it differs from the original session (cross-device resume). **401** means the guest cookie is missing or the token session is stale (e.g. after a successful rebind on another device). Handle **`isResumable`**, **`reasonIfNotResumable`**, and non-resumable **400** responses in the UI.

---

## 7. Minimal client state shape (suggested)

```ts
type QuoteJourneyClientState = {
  quoteId: string;
  version: number;      // from quote.version; use for If-Match
  etag: string | null;  // mirror ETag / version for If-None-Match on GET
  journey: QuoteJourneyState;
  quote: QuoteSnapshotDto;
};
```

Update **`version`** + **`etag`** together on every **200** from init, journey-state, or PATCH.

---

## 8. QA checklist

- [ ] PATCH never sent without **`If-Match`**.
- [ ] After each PATCH **200**, **`quote.version`** updates and the next PATCH uses it.
- [ ] **412** → refetch journey, user can retry without losing server truth.
- [ ] **GET journey-state** with **`If-None-Match`** → **304** does not wipe UI.
- [ ] Back navigation shows data from **`quote`**, not empty defaults.
- [ ] **locked** step cannot be used as primary flow; redirects to resume path.
- [ ] Double **Next** does not double PATCH (in-flight guard).
