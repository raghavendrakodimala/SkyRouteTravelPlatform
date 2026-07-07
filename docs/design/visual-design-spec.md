# SkyRoute Visual Design Spec — Type, Color, Spacing, Forms, Buttons

| Field | Value |
|---|---|
| Document ID | DESIGN-VISUAL-001 |
| Date | 2026-07-07 |
| Author | ux-ui-designer |
| Branch | `fix/requirements-compliance-gaps-skyroute-mvp` |
| Phase | Requirements Compliance Fix (post-Phase-17) |
| Status | Implemented (2026-07-07) — applied CSS-only across `frontend/src/styles.css`, `app.css`, and the feature component stylesheets |
| Scope | Visual/CSS polish only. No new components, no behavior changes, no new dependencies (no UI/component library, no external fonts). |
| Trigger | Human Product Owner live-tested the app in Chrome and rated the UI "poorly designed" — specifically visual polish (layout, spacing, colors, typography), not booking-form UX or search/results layout logic. |

---

## 0. Verification Against Actual Source

Before drafting this spec, every CSS file under `frontend/src/**/*.css` and every template under `frontend/src/app/features/**/*.html` (plus `frontend/src/app/app.css`/`app.html`, `frontend/src/styles.css`, `frontend/src/index.html`) was read directly. Findings, confirmed against source (not assumed from the summary):

1. **Font mismatch — confirmed.** `grep -r "font-family" frontend/src` returns **zero matches** anywhere in the codebase. No component CSS and no global stylesheet sets a `font-family`. Every `<h1>`/`<h2>`/`<h3>` therefore falls back to the browser's default serif font (e.g., Georgia/Times New Roman in Chrome), while every other element (labels, inputs, buttons, paragraphs) falls back to the browser's default sans-serif font. This is exactly as described — an unintentional pairing, not a deliberate design choice.
2. **Unstyled native controls — confirmed.** `search-form.component.css` (`select, input { padding: 0.5rem; font-size: 1rem; }`), `passenger-form-section.component.css` (`input { padding: 0.5rem; font-size: 1rem; }`) — no `border`, no `border-radius`, no `background`, no `:focus` treatment anywhere. Zero occurrences of `outline` or `:focus` in `frontend/src` (grep-confirmed) — the browser's default control chrome and default focus ring are the only visual treatment present today.
3. **No card/container system — confirmed, but partial.** `.result-card` (results-list) and `.price-breakdown` (booking-form) do have a border + `border-radius` + (for `.price-breakdown`) a light background. `.passenger-section` (a `<fieldset>`) has a border + radius but no background. But `.flight-summary` (both `booking-form` and `confirmation`) has **no border, no background, no radius at all** — it is a bare text block. The four containers are also inconsistent with each other: radii of 4px (`.price-breakdown`, `.passenger-section`) vs. 6px (`.result-card`) vs. none (`.flight-summary`); no shadows anywhere.
4. **Header — confirmed.** `app.css`: `.app-header { background: #1a5fb4; color: #fff; }`. This is the only deliberate color statement in the app outside of status colors (error red, warning yellow) and the confirmation screen's booking-reference badge.
5. **Buttons — confirmed.** Every `button` rule across all five component stylesheets (`search-form`, `results-list`, `booking-form`, `confirmation`, `sort-control` is the one exception, see below) sets only `padding`, `font-size`, `cursor`, and a `:disabled` opacity — no `background`, `border`, `border-radius`, or `:hover`/`:focus` treatment, so they render as the browser's default grey 3D-bevel button. The one exception already in the codebase is `sort-control.component.css`'s `.sort-option`/`.sort-option.active`, which already has a real border, radius, and an active-state fill in the brand blue — this is the one place in the app that already resembles a "designed" control and is used below as a pattern to extend, not replace.

**CSP note (relevant to the type system below):** `frontend/src/index.html`'s `Content-Security-Policy` meta tag sets `font-src 'self'` and `style-src 'self' 'unsafe-inline'` — there is no allowance for a remote font host (e.g., Google Fonts). This independently confirms the constraint already given: the type system below must use only local/system font stacks, no `@font-face` remote imports, no new dependency.

---

## 1. Design Tokens

Add this block to `frontend/src/styles.css`, inside a new (or the existing) `:root` selector. Every other rule in this spec references these custom properties — do not hard-code the hex/px values a second time anywhere else.

```css
:root {
  color-scheme: light; /* existing — keep, owned by the concurrent dark-mode fix */

  /* ---- Color: brand ---- */
  --color-primary: #1a5fb4;         /* existing brand blue — unchanged, reused everywhere below */
  --color-primary-hover: #164e93;   /* ~15% darker, for hover states */
  --color-primary-active: #123f78;  /* ~30% darker, for :active/pressed states */
  --color-primary-tint: #eef4fc;    /* existing confirmation booking-reference background */
  --color-link: var(--color-primary);

  /* ---- Color: neutrals ---- */
  --color-white: #ffffff;
  --color-neutral-900: #1a1a1a;     /* body/heading text — near-black, softer than pure #000 */
  --color-neutral-700: #444444;     /* existing .timing text color */
  --color-neutral-600: #555555;     /* existing .per-person-price / .lead-badge / .required-indicator color */
  --color-neutral-400: #999999;     /* existing sort-option inactive border */
  --color-neutral-300: #cccccc;     /* existing passenger-section border */
  --color-neutral-200: #dddddd;     /* existing result-card / price-breakdown border */
  --color-neutral-100: #f2f4f7;     /* new — disabled form-control background only */
  --color-border-control: #767676;  /* new — form input/select border, see §4 contrast note */
  --color-surface-subtle: #f7f9fc; /* existing price-breakdown background */

  /* ---- Color: status (all existing values, reused as-is) ---- */
  --color-error: #b00020;
  --color-error-bg: #fdecea;
  --color-warning-text: #665c00;
  --color-warning-bg: #fff8e1;
  --color-warning-border: #e0c200;

  /* ---- Typography ---- */
  --font-family-base: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial,
    sans-serif, "Apple Color Emoji", "Segoe UI Emoji";
  --font-size-xs: 0.75rem;     /* 12px */
  --font-size-sm: 0.875rem;    /* 14px */
  --font-size-base: 1rem;      /* 16px */
  --font-size-lg: 1.125rem;    /* 18px */
  --font-size-xl: 1.25rem;     /* 20px */
  --font-size-2xl: 1.5rem;     /* 24px */
  --font-size-3xl: 1.875rem;   /* 30px */
  --font-size-4xl: 2rem;       /* 32px — existing booking-reference size */
  --line-height-base: 1.5;
  --font-weight-regular: 400;
  --font-weight-semibold: 600;
  --font-weight-bold: 700;
  --font-weight-extrabold: 800; /* existing booking-reference weight */

  /* ---- Spacing (4px rhythm) ---- */
  --space-1: 4px;
  --space-2: 8px;
  --space-3: 12px;
  --space-4: 16px;
  --space-5: 24px;
  --space-6: 32px;
  --space-7: 48px;

  /* ---- Radius ---- */
  --radius-sm: 4px;   /* form controls, buttons, alert banners */
  --radius-md: 6px;   /* cards / sections */
  --radius-lg: 8px;   /* confirmation booking-reference badge only */

  /* ---- Elevation ---- */
  --shadow-sm: 0 1px 2px rgba(26, 26, 26, 0.06);
  --shadow-md: 0 2px 6px rgba(26, 26, 26, 0.08);

  /* ---- Motion ---- */
  --transition-fast: 150ms ease;

  /* ---- Misc ---- */
  --opacity-disabled: 0.6; /* existing button:disabled value, unchanged */
}

/* Required alongside the tokens: once form controls gain border + padding, this prevents them
   from overflowing their flex containers (every `.field` in search-form and
   passenger-form-section is `display:flex; flex-direction:column`, so its input/select children
   already stretch to the container's content-box width today; adding border+padding without
   border-box sizing would push them wider than the container). */
*,
*::before,
*::after {
  box-sizing: border-box;
}
```

---

## 2. Type System

**One font family for the whole app** (headings and body both) — this directly removes the accidental serif/sans-serif mismatch, and stays inside the `font-src 'self'` CSP constraint since every name in the stack resolves to a locally installed OS font, never a network request.

```css
--font-family-base: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial,
  sans-serif, "Apple Color Emoji", "Segoe UI Emoji";
```

Apply globally in `frontend/src/styles.css`, extending the **existing** `html, body` rule (do not duplicate the selector, and do not remove or reorder the existing `background-color: #ffffff` line — that belongs to the concurrent dark-mode fix):

```css
html,
body {
  background-color: #ffffff; /* existing line — keep as-is */
  font-family: var(--font-family-base);
  font-size: var(--font-size-base);
  line-height: var(--line-height-base);
  color: var(--color-neutral-900);
}

h1, h2, h3, h4, h5, h6 {
  font-family: var(--font-family-base); /* removes the serif fallback everywhere */
  font-weight: var(--font-weight-bold);
  color: var(--color-neutral-900);
  margin: 0 0 var(--space-4);
}

h1 { font-size: var(--font-size-3xl); } /* 30px — page-purpose headings */
h2 { font-size: var(--font-size-xl); }  /* 20px */
h3 { font-size: var(--font-size-lg); }  /* 18px */
```

`.app-header h1` in `app.css` already has a more specific selector (`.app-header h1` beats a bare `h1`), so it keeps overriding the generic rule above — just retokenize it, don't remove it:

```css
.app-header h1 {
  margin: 0;
  font-size: var(--font-size-2xl); /* 24px — unchanged value, now a token */
}
```

### Body/secondary text sizing (apply where each selector already exists — values are unchanged unless noted, only the unit source changes)

| Selector | File | New size | Note |
|---|---|---|---|
| `label`, form text, buttons | all form CSS | `var(--font-size-base)` (16px) | unchanged |
| `.error` | search-form, passenger-form-section | `var(--font-size-sm)` (14px) | was 0.85rem (13.6px), negligible size change, color unchanged |
| `.required-indicator` | search-form, passenger-form-section | `var(--font-size-sm)` (14px) | was 0.85rem, color unchanged (#555) |
| `.timing` | results-list | `var(--font-size-sm)` (14px) | was 0.9rem (14.4px), color unchanged (#444) |
| `.per-person-price`, `.lead-badge` | results-list, booking-form, passenger-form-section | `var(--font-size-sm)` (14px) | was 0.85–0.95rem, color unchanged (#555) |
| `legend` | passenger-form-section | `var(--font-size-lg)` (18px), `var(--font-weight-bold)` | **new explicit size** — today it inherits the browser's (often small) default `<legend>` size; this makes "Passenger N" read as a proper sub-heading |
| `.total-price` | results-list, booking-form, confirmation | `var(--font-size-2xl)` (24px) | **harmonization**: today this is 1.3rem/1.5rem/1.4rem (20.8/24/22.4px) — three different sizes for the same semantic element across three screens. Standardize to one size everywhere. |
| `.booking-reference` | confirmation | `var(--font-size-4xl)` (32px), `var(--font-weight-extrabold)` | unchanged — keep exactly as-is (this is the tightest contrast margin pair, see §3) |

No font-size change here alters any color, so none of these changes affects any contrast ratio (contrast is a function of the two colors only, not text size) — see §3 for the explicit contrast re-check.

---

## 3. Color Palette and Accessibility Contrast Check

The palette is anchored on the existing brand blue `#1a5fb4` and reuses every color already verified in `docs/reviews/accessibility-review-phase-17.md`'s contrast table wherever a pairing already exists, rather than inventing new hex values for the same role. Full token list is in §1.

### 3.1 Cross-check against the Phase 17 contrast table

Every row in the review's table (reproduced below) uses a color pair that is **only renamed to a token in this spec, never changed**:

| Table row | Existing hex pair | Token mapping | Status |
|---|---|---|---|
| App header `<h1>` | `#ffffff` on `#1a5fb4` | `--color-white` on `--color-primary` | Unchanged — 6.29:1 |
| Field/banner error text | `#b00020` on `#ffffff` | `--color-error` on `--color-white` | Unchanged — 7.32:1 |
| `.banner-error` tint | `#b00020` on `#fdecea` | `--color-error` on `--color-error-bg` | Unchanged — ≈6.9:1 |
| `.banner-info` | `#665c00` on `#fff8e1` | `--color-warning-text` on `--color-warning-bg` | Unchanged — 6.38:1 |
| `.timing` | `#444444` on `#ffffff` | `--color-neutral-700` on `--color-white` | Unchanged — 9.73:1 |
| `.per-person-price` | `#555555` on `#ffffff`/`#f7f9fc` | `--color-neutral-600` on `--color-white`/`--color-surface-subtle` | Unchanged — 7.46:1 |
| `.lead-badge` | `#555555` on `#ffffff` | `--color-neutral-600` on `--color-white` | Unchanged — 7.46:1 |
| `.sort-option.active` | `#ffffff` on `#1a5fb4` | `--color-white` on `--color-primary` | Unchanged — 6.29:1 |
| Booking reference | `#1a5fb4` on `#eef4fc` | `--color-primary` on `--color-primary-tint` | Unchanged — 5.69:1 |

**No hex value in the table above changes.** Every one is simply given a name.

### 3.2 New pairs introduced by this spec

Only three genuinely new pairings are introduced (everything else in the spec reuses a table row's existing hex pair, e.g. the secondary-button hover state below reuses the exact `#1a5fb4` on `#eef4fc` pair already verified at 5.69:1). Computed by the same sRGB relative-luminance method the Phase 17 review used:

| New pair | Used for | Computed ratio | Threshold | Result |
|---|---|---|---|---|
| `--color-neutral-900` (`#1a1a1a`) on `--color-white` (`#ffffff`) | default body/heading text (replaces implicit browser-default pure black) | ≈17.4:1 | 4.5:1 (normal text) | Pass, large margin |
| `--color-white` (`#ffffff`) on `--color-primary-hover` (`#164e93`) | primary button hover/active background with white label text | ≈8.3:1 | 4.5:1 (normal text) | Pass — darker than the already-passing base primary, contrast can only increase |
| `--color-border-control` (`#767676`) on `--color-white` (`#ffffff`) | new input/select border | ≈4.5:1 | 3:1 (WCAG 1.4.11 non-text/UI-component contrast — not one of the six SCs the Phase 17 review scoped, but part of the same WCAG 2.1 AA baseline `docs/requirements.md` §5.5 commits to generally) | Pass |

The border-color choice deserves a short explanation: the two existing borders already in the app (`#dddddd` on cards, `#999999` on the sort control) compute at roughly 1.4:1 and 2.85:1 against white — both below the 3:1 non-text-contrast guideline. Neither was flagged by the Phase 17 review because 1.4.11 was not one of the six SCs it scoped, and this spec does not require touching those two existing values (out of scope — they are card/toggle borders already shipped and reviewed with zero findings). But this spec is *introducing a new* border value for the first time on interactive form fields, and picking one that clears 3:1 both (a) genuinely helps the "looks unstyled" complaint — a border needs to be visible to read as a deliberate field boundary — and (b) costs nothing, so `#767676` was chosen over a lighter, more-common-but-barely-visible `#cccccc`/`#dddddd`-style value.

**No new pair anywhere in this spec falls below 4.5:1 normal-text / 3:1 large-text or introduces a regression against the Phase 17 table.**

---

## 4. Spacing Scale and Card/Container Treatment

### 4.1 Spacing

The existing codebase already, almost entirely by accident, uses `rem` values that map cleanly onto a 4px rhythm (0.25rem=4px, 0.5rem=8px, 0.75rem=12px, 1rem=16px, 1.5rem=24px, 2rem=32px). This spec formalizes that into named tokens rather than changing the actual computed spacing in most places — call out below wherever a value is deliberately being *harmonized* (changed) rather than just renamed.

| Token | Value | Primary uses |
|---|---|---|
| `--space-1` | 4px | label-to-field gap (unchanged) |
| `--space-2` | 8px | form control internal padding (vertical), sort-option gap |
| `--space-3` | 12px | form control internal padding (horizontal), button vertical padding |
| `--space-4` | 16px | card/section internal padding, `.field` margin-bottom, heading margin-bottom |
| `--space-5` | 24px | page container padding, button horizontal padding, section margin-bottom |
| `--space-6` | 32px | page container top/bottom margin |
| `--space-7` | 48px | reserved, not required by this pass |

**Deliberate harmonization (flagged, not a pure rename):** `passenger-form-section.component.css`'s `.field { margin-bottom: 0.75rem; }` (12px) becomes `var(--space-4)` (16px), to match `search-form.component.css`'s `.field { margin-bottom: 1rem; }` (already 16px). This is the one spacing value that differs between the two forms today for no evident reason; unify to 16px.

### 4.2 Card/container treatment

Apply this **exact same recipe** to `.result-card`, `.price-breakdown`, `.flight-summary` (both booking-form and confirmation), and `.passenger-section` — reusing each selector's own existing name (do not introduce a shared `.card` class; that would require template edits to add the class, which is out of scope for a CSS-only change):

```css
/* apply to: .result-card, .price-breakdown, .flight-summary, .passenger-section */
border: 1px solid var(--color-neutral-200); /* #dddddd — existing card/price-breakdown value */
border-radius: var(--radius-md);            /* 6px — existing .result-card value */
padding: var(--space-4);                    /* 16px — already the value on 3 of the 4 selectors */
margin-bottom: var(--space-4);              /* 16px */
background: var(--color-white);
box-shadow: var(--shadow-sm);               /* new — the one genuinely new visual addition */
```

Selector-by-selector delta from current state:

- **`.result-card`** (results-list): border color/radius/padding already match this recipe almost exactly (`#ddd`, 6px, 1rem) — only `box-shadow` and explicit `background: var(--color-white)` are new.
- **`.price-breakdown`** (booking-form): border-radius changes from 4px → 6px (`--radius-md`, to match `.result-card`); background `#f7f9fc` stays as-is (do **not** overwrite with white — the light-tint background is a deliberate, already-reviewed, already-passing distinguishing treatment for this one "price" block, keep it); add `box-shadow`.
- **`.flight-summary`** (booking-form and confirmation): **this selector currently has zero card styling at all** — this is a straightforward "add the whole recipe," the most visible single change in this spec. `confirmation.component.css`'s `.flight-summary` additionally keeps its existing `text-align: left;`.
- **`.passenger-section`** (passenger-form-section, a `<fieldset>`): border-radius changes from 4px → 6px; border color stays `#ccc` (`--color-neutral-300`, not `--color-neutral-200`, since that's the existing value already reviewed — do not conflate the two greys); add `background: var(--color-white)` and `box-shadow`.

**Explicitly out of scope for the card treatment:** the four `-page` wrapper selectors (`.search-form-page`, `.results-page`, `.booking-page`, `.confirmation-page`) stay plain layout containers (`max-width`/`margin`/`padding` only, retokenized but not bordered) — they sit directly on the white page background, so a white-bordered card nested inside a white, unbordered page wrapper would be the correct visual hierarchy (the *inner* sections read as distinct cards; the outer wrapper is just a content-width constraint). Also out of scope: `.passengers` on the confirmation screen (not named in the task's target list) — leave it as plain text, just retokenize its heading/list font sizes.

---

## 5. Form Controls and Buttons

### 5.1 Form controls (`select`, `input` in `search-form.component.css` and `passenger-form-section.component.css`)

```css
select,
input {
  font-family: inherit;
  font-size: var(--font-size-base);
  padding: var(--space-2) var(--space-3); /* 8px 12px */
  border: 1px solid var(--color-border-control); /* #767676, see §3.2 */
  border-radius: var(--radius-sm); /* 4px */
  background: var(--color-white);
  color: var(--color-neutral-900);
  transition: border-color var(--transition-fast), box-shadow var(--transition-fast);
}

select:focus,
input:focus {
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px rgba(26, 95, 180, 0.25); /* soft brand-blue glow, additive */
  /* Do NOT add `outline: none` here or anywhere else — the native focus outline this
     glow supplements is the exact mechanism the Phase 17 review confirmed is present
     and un-removed everywhere in the app ("Focus-visible/outline removal: zero matches
     found"). This box-shadow is in addition to, never a replacement for, that outline. */
}

/* CSS-only invalid-state cue: Angular reactive forms already apply `.ng-invalid`/`.ng-touched`
   to the native element automatically — no template or TypeScript change required to add this. */
select.ng-invalid.ng-touched,
input.ng-invalid.ng-touched {
  border-color: var(--color-error);
}
```

Do **not** set `appearance: none` on `<select>` — that would require replacing the native dropdown arrow with a custom background image (a new asset), and risks inconsistent native behavior across browsers. Border, radius, padding, background, and focus/invalid states are all safe to style directly without touching `appearance`, and preserve the native control the Phase 17 accessibility review already confirmed is fully keyboard-operable and correctly labeled.

`<input type="date">` accepts this same treatment cleanly in Chrome (the browser the Product Owner tested in); no date-picker-specific styling is included in this spec.

### 5.2 Buttons

Two visual tiers, both reusing the same brand blue, differentiated by fill vs. outline — reserved for a clear reason (see rationale below), not an arbitrary split:

- **Primary (filled):** "Search" (search-form), "Confirm Booking" (booking-form), "Start a New Search" (confirmation) — each is the single, unambiguous next step on its screen.
- **Secondary (outline):** "Select" (results-list) — this button repeats once per result card; filling every one of N cards with solid blue would visually overwhelm the list and de-emphasize the one truly primary action per screen. An outline treatment keeps each "Select" clearly actionable without competing with itself down the list.

```css
/* shared base — apply in each of the four button-owning stylesheets */
button {
  font-family: inherit;
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-semibold);
  padding: var(--space-3) var(--space-5); /* 12px 24px */
  border-radius: var(--radius-sm); /* 4px */
  cursor: pointer;
  transition: background-color var(--transition-fast), border-color var(--transition-fast),
    color var(--transition-fast);
}

button:disabled {
  cursor: not-allowed;
  opacity: var(--opacity-disabled); /* 0.6 — unchanged value, now a token.
    WCAG 1.4.3/1.4.11 explicitly exempt inactive/disabled components from contrast
    requirements, so no ratio re-check is needed for this state. */
}

/* Primary — search-form "Search", booking-form "Confirm Booking", confirmation "Start a New Search" */
button.primary /* or the bare `button` selector in files with no secondary button, e.g. booking-form/confirmation */ {
  background: var(--color-primary);
  color: var(--color-white);
  border: 1px solid var(--color-primary);
}
button.primary:hover:not(:disabled) {
  background: var(--color-primary-hover);
  border-color: var(--color-primary-hover);
}
button.primary:active:not(:disabled) {
  background: var(--color-primary-active);
  border-color: var(--color-primary-active);
}

/* Secondary — results-list "Select" only */
button.secondary {
  background: var(--color-white);
  color: var(--color-primary);
  border: 1px solid var(--color-primary);
}
button.secondary:hover:not(:disabled) {
  background: var(--color-primary-tint); /* #eef4fc — reuses the exact pair verified at 5.69:1 in §3.1 */
}

button:focus-visible {
  outline: 2px solid var(--color-primary);
  outline-offset: 2px;
  /* additive, never a replacement — see the note in §5.1 */
}
```

Since `search-form.component.css` and `booking-form.component.css` each have exactly one `button` selector today (no distinction needed in-file), the primary rule can simply be applied to the bare `button` selector in those two files. `results-list.component.css`'s `button` selector should get the secondary treatment instead (it is also the only button in that file). `confirmation.component.css`'s single `button` gets the primary treatment.

### 5.3 Sort control (`sort-control.component.css`) — extend the existing pattern, don't replace it

This component already has the most "designed" treatment in the app. Retokenize its existing values (no value changes) and add one missing state — a hover cue on the inactive options, which today have none:

```css
.sort-control {
  gap: var(--space-2); /* unchanged */
}

.sort-option {
  padding: var(--space-2) var(--space-3); /* was 0.4rem/0.8rem (6.4/12.8px) — unify to 8/12px */
  font-size: var(--font-size-sm);
  border: 1px solid var(--color-neutral-400); /* #999999 — unchanged */
  border-radius: var(--radius-sm); /* unchanged */
  background: var(--color-white);
  color: var(--color-neutral-700); /* new explicit value (#444444, the same already-verified
    .timing color) instead of the inherited browser-default black */
  transition: background-color var(--transition-fast), border-color var(--transition-fast),
    color var(--transition-fast);
}

.sort-option:not(.active):hover {
  background: var(--color-primary-tint);
  border-color: var(--color-primary);
  color: var(--color-primary);
}

.sort-option.active {
  background: var(--color-primary); /* unchanged */
  color: var(--color-white); /* unchanged */
  border-color: var(--color-primary); /* unchanged */
  font-weight: var(--font-weight-semibold); /* unchanged */
}
```

---

## 6. File-by-File Implementation Map

| File | Changes |
|---|---|
| `frontend/src/styles.css` | Add the `:root` token block (§1) and the `box-sizing` reset (§1) to the existing file. Extend the existing `html, body` rule with `font-family`/`font-size`/`line-height`/`color` (§2) — do not touch its existing `background-color: #ffffff` line. Add global `h1`–`h6` rules (§2). Add a global `a`/`a:hover`/`a:focus-visible` rule reusing `--color-link` (no `<a>` elements exist in the templates today, but this satisfies "applied consistently to buttons, links, and accents" for when one is added) and a global `button:focus-visible`/form-control `:focus-visible` rule if not colocated per-component (§5.1/§5.2). |
| `frontend/src/app/app.css` | Retokenize `.app-header` (`background: var(--color-primary)`, padding via `--space-4`/`--space-5`) and `.app-header h1` (`font-size: var(--font-size-2xl)`). Optional: add `box-shadow: var(--shadow-sm)` to `.app-header` for a hairline of depth under the bar — not required, PO's complaint didn't target the header. |
| `frontend/src/app/features/search/search-form/search-form.component.css` | `.search-form-page` retokenize padding/margin (§4.1). `.field` margin-bottom `--space-4` (unchanged value). `select, input` full form-control treatment (§5.1). `button` primary treatment (§5.2). `.error`/`.required-indicator`/`.banner-error` retokenize font-size/colors/radius (values unchanged, see §2/§3.1). |
| `frontend/src/app/features/results/results-list/results-list.component.css` | `.results-page` retokenize. `.result-card` card treatment (§4.2, adds shadow). `.timing`/`.per-person-price` retokenize (§2). `.total-price` → `--font-size-2xl` (harmonization, §2). `button` secondary treatment (§5.2). `.banner-error` retokenize. |
| `frontend/src/app/features/booking/booking-form/booking-form.component.css` | `.booking-page` retokenize. `.flight-summary` **add full card treatment** (currently has none — see §4.2, the most visible change in this file). `.price-breakdown` card treatment, radius 4px→6px (§4.2). `.total-price` → `--font-size-2xl` (harmonization). `button` primary treatment. `.banner-error`/`.banner-info` retokenize. |
| `frontend/src/app/features/booking/passenger-form-section/passenger-form-section.component.css` | `.passenger-section` card treatment, radius 4px→6px (§4.2). `legend` new explicit `font-size: var(--font-size-lg)` (§2). `.field` margin-bottom 12px→16px (harmonization, §4.1). `input` full form-control treatment (§5.1). `.error`/`.required-indicator`/`.lead-badge` retokenize. |
| `frontend/src/app/features/confirmation/confirmation/confirmation.component.css` | `.confirmation-page` retokenize. `.booking-reference` retokenize only (all values unchanged, see §2 table note on why this one is untouched). `.flight-summary` **add full card treatment** (currently has none, keep existing `text-align: left`). `.total-price` → `--font-size-2xl` (harmonization). `button` primary treatment. `.passengers` retokenize heading/list font sizes only, no card treatment (out of scope, §4.2). |
| `frontend/src/app/features/results/sort-control/sort-control.component.css` | Retokenize existing values (no changes) and add the missing inactive-option `:hover` state (§5.3). |

No `.html` template file needs any edit for this spec — every change above is achievable through the existing class names and Angular's automatically-applied `.ng-invalid`/`.ng-touched` classes.

---

## 7. Explicit Constraints (repeated from the task, for the implementer)

- CSS-only. No new Angular components, directives, or template structural changes.
- No new dependencies — no icon font, no UI component library, no CSS framework, no remote web font. The type system in §2 is chosen specifically to satisfy this with a system-font stack.
- No behavior changes — every interactive element stays the same native `<button>`/`<select>`/`<input>` it is today; only visual treatment changes.
- Do not touch `html, body`'s existing `background-color: #ffffff` declaration or the `color-scheme: light` rule in `frontend/src/styles.css` — both are owned by a concurrent dark-mode-inversion fix landing separately on this same branch. Coordinate before editing that block if it has moved by implementation time.
- Do not remove or override the native browser focus outline (`outline: none`/`outline: 0`) anywhere — every `:focus-visible` addition in this spec is additive, preserving the zero-findings state the Phase 17 accessibility review confirmed for focus visibility.
- Do not add `appearance: none` to `<select>` elements.
- Every existing WCAG contrast pass in `docs/reviews/accessibility-review-phase-17.md`'s table is reused unchanged (§3.1); the three genuinely new pairs introduced (§3.2) were computed and all clear their applicable threshold with margin.

## 8. Open Questions / Decisions Needing a Second Look

1. **Page backdrop.** This spec deliberately keeps the outer page background pure white and relies on `border` + `box-shadow` alone to make cards read as distinct from the page (§4.2), specifically to avoid touching the `html, body` background rule the concurrent dark-mode fix owns. If, after that fix lands, the team wants a subtle off-white page backdrop (e.g., `--color-surface-subtle`, `#f7f9fc`, already in the token list) behind white cards for a stronger separation, that is a one-line follow-up once the two branches are reconciled — flagging it here rather than doing it now to avoid a merge collision on that specific line.
2. **`.passengers` on the confirmation screen** was left out of the card treatment because the task's target list (`.result-card`, `.price-breakdown`, `.flight-summary`, "the passenger fieldset", "form field groups") doesn't name it. Product Owner/engineer may want it included for visual consistency with `.passenger-section` on the booking screen — low-risk, same recipe as §4.2, one-line addition if wanted.
3. **`--color-border-control` (`#767676`)** for form-control borders is a genuinely new value, chosen to clear the WCAG 1.4.11 non-text-contrast guideline (§3.2) even though that specific success criterion sits outside the six SCs the Phase 17 review scoped. If the team prefers a lighter, more "flat modern" border (e.g., reusing the existing `#dddddd` card border) purely for aesthetics, that's a valid alternative call — it would just reintroduce the same sub-3:1 boundary contrast the two existing borders already have (out-of-scope today, not a regression, just not an improvement either).

## 9. Relevant Files Read During Verification

- `frontend/src/styles.css`, `frontend/src/app/app.css`, `frontend/src/app/app.ts`, `frontend/src/app/app.html`, `frontend/src/index.html`
- `frontend/src/app/features/search/search-form/search-form.component.css`, `.html`
- `frontend/src/app/features/results/results-list/results-list.component.css`, `.html`
- `frontend/src/app/features/results/sort-control/sort-control.component.css`, `.html`
- `frontend/src/app/features/booking/booking-form/booking-form.component.css`, `.html`
- `frontend/src/app/features/booking/passenger-form-section/passenger-form-section.component.css`, `.html`
- `frontend/src/app/features/confirmation/confirmation/confirmation.component.css`, `.html`
- `docs/reviews/accessibility-review-phase-17.md` (contrast table, §3.1 cross-check)

*End of Visual Design Spec.*
