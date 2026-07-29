# Dev Description

<!--
  For every checked box below, add a short explanation (1-2 sentences) of
  HOW it was satisfied — not just that it was. Unchecked boxes should say
  why (e.g. "N/A — no UI changes in this MR").
-->

## Dev Checklist

- [ ] **Unit tests are present and pass**
  - _Explanation:_
- [ ] **Integration tests are present and pass**
  - _Explanation:_
- [ ] **Acceptance tests are present and pass**
  - _Explanation:_
- [ ] **Manual tests are created and tested**
  - _Explanation:_ (what was tested, and how — steps or scenarios)
- [ ] **No code smells** — checked for:
  - Naming: clear, intention-revealing names (no `data2`, `temp`, `flag`, single-letter names outside tight loops)
  - Function/method size & responsibility: each function does one thing (rough guide: >40-50 lines is a flag)
  - Duplication (DRY): no copy-pasted logic; repeated code extracted into shared functions/utilities
  - Magic numbers/strings: no unexplained literals; constants are named
  - Nesting/complexity: no deeply nested conditionals/loops (rough guide: >3 levels is a flag); early returns/guard clauses used
  - Dead code: no commented-out code, unused imports/variables, or unreachable branches
  - Error handling: errors handled explicitly, not swallowed silently (no empty catch blocks)
  - Consistent style: follows the project's existing formatting/linting conventions
  - _Explanation:_ (note which of the above you actually reviewed/changed)
- [ ] **Relevant docstrings**
  - _Explanation:_ (which methods/classes were documented)
- [ ] **Affected ACs are included in the MR description**
  - _Explanation:_ (list the AC IDs)

---

## Reviewer Checklist

- [ ] **No code smells** — check for: naming, function size/responsibility, duplication (DRY), magic numbers/strings, nesting/complexity, dead code, error handling, consistent style (see dev checklist above for details)
- [ ] **Unit tests**
  - Is there enough unit test coverage and do they all pass?
- [ ] **Integration tests**
  - Is there enough integration test coverage and do they all pass?
- [ ] **Relevant docstrings**
  - Do all new/changed methods have docstrings?
- [ ] **Passes acceptance criteria**
  - Do all ACs touched in this branch pass the specification from the sprint backlog?
- [ ] **Protected against sneaky inputs or malicious activity**
  - Is the app protected against SQL injection, HTML/script injection (XSS), and edge-case inputs (e.g. whitespace trimmed, empty strings, overly long inputs, null/undefined)?

### NFR Checklist (Reviewer)

- [ ] **NFR 2: Accessible and consistent UI**
  - Colours and fonts remain consistent across pages.
  - The application is responsive across screen sizes (mobile → desktop).
  - Menus, buttons, links, and input fields behave consistently across pages (placement, interaction, and labelling).

- [ ] **NFR 3: User-friendly and fool-proof interaction**
  - Required fields and input formats are clearly indicated.
  - Invalid inputs or errors are visually highlighted to help users correct them.
  - All form errors are shown at once during submission.
  - Destructive actions (e.g., deletion) require explicit confirmation.
  - User input is preserved after errors (except for passwords).

- [ ] **NFR 4: Character support**
  - The product accepts all valid characters, including accented letters (e.g., Māori) and emojis in open text fields.

- [ ] **NFR 5: Keyboard navigation and accessibility**
  - Pressing **Tab** moves users sequentially through interactive elements to support keyboard navigation and screen readers.

- [ ] **NFR 6: Locale-aware formatting**
  - The system displays formatted data (e.g., dates and times) according to the user's locale.
