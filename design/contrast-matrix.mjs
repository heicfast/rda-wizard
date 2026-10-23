#!/usr/bin/env node
// design/contrast-matrix.mjs — compute WCAG 2.2 contrast ratios for the design system's
// critical token pairs, per theme. Emits docs/ACCESSIBILITY.md §contrast tables and
// FAILS (exit 1) if any AA-critical pair is below 4.5:1 (3:1 for large/18px+ text).
// Evidence chain: tokens.json values (QSS/TDI-sourced); [ADV] pairs documented inline.
import fs from 'node:fs';
import path from 'node:path';

const ROOT = path.resolve(path.dirname(process.argv[1]), '..');
const T = JSON.parse(fs.readFileSync(path.join(ROOT, 'design/tokens.json'), 'utf8'));

const colors = {};
for (const group of Object.values(T.colors)) {
  for (const t of group) colors[t.key] = t;
}
const get = (key, theme) => {
  const v = colors[key]?.[theme];
  if (!v || v.startsWith('@')) return null; // SystemColors — OS-managed, excluded
  return v;
};

const lum = (hex) => {
  const h = hex.replace('#', '');
  const full = h.length === 6 ? 'FF' + h : h;
  const [r, g, b] = [2, 4, 6].map(i => parseInt(full.slice(i, i + 2), 16) / 255)
    .map(c => c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4);
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
};
const ratio = (a, b) => {
  const la = lum(a), lb = lum(b);
  const [hi, lo] = la >= lb ? [la, lb] : [lb, la];
  return (hi + 0.05) / (lo + 0.05);
};
const hex8 = (v) => {
  const h = v.replace('#', '');
  return h.length === 6 ? 'FF' + h : h;
};
const blend = (v, baseHex) => { // composite alpha hex over base (for tint tokens)
  const f = hex8(v), b = hex8(baseHex);
  const a = parseInt(f.slice(0, 2), 16) / 255;
  const mix = (i) => Math.round(a * parseInt(f.slice(i, i + 2), 16) + (1 - a) * parseInt(b.slice(i, i + 2), 16));
  return '#' + [2, 4, 6].map(i => mix(i).toString(16).padStart(2, '0')).join('').toUpperCase();
};

// Critical pairs: [foreground, background, role, minRatio, note]
// Window/surface bases for alpha compositing.
const BASE = { light: '#FFFFFF', dark: '#1B1D1F' };
const pairs = [
  ['Brush.Text.Primary', 'Brush.Bg.Window', 'body text on window', 4.5],
  ['Brush.Text.Primary', 'Brush.Bg.Canvas', 'body text on canvas', 4.5],
  ['Brush.Text.Primary', 'Brush.Bg.Card', 'body text on card', 4.5],
  ['Brush.Text.Primary', 'Brush.Bg.CardHover', 'body text on hover card', 4.5],
  ['Brush.Text.Secondary', 'Brush.Bg.Window', 'secondary text on window', 4.5],
  ['Brush.Text.Secondary', 'Brush.Bg.Canvas', 'secondary text on canvas', 4.5],
  ['Brush.Text.Secondary', 'Brush.Bg.Card', 'secondary text on card', 4.5],
  ['Brush.Text.Secondary', 'Brush.Bg.Surface', 'secondary text on surface', 4.5],
  ['Brush.Text.Tertiary', 'Brush.Bg.Window', 'tertiary/placeholder text', 3.0, 'large/auxiliary only'],
  ['Brush.Text.Disabled', 'Brush.Bg.Window', 'disabled text (exempt)', 1.0, 'WCAG exempts disabled'],
  ['Brush.Text.Link', 'Brush.Bg.Window', 'link text', 4.5],
  ['Brush.Text.Link', 'Brush.Bg.Canvas', 'link text on canvas', 4.5],
  ['Brush.Brand.On', 'Brush.Brand.Primary', 'text on primary button', 4.5],
  ['Brush.Status.DangerOn', 'Brush.Status.DangerFill', 'danger button text', 4.5],
  ['Brush.Brand.SecondaryFg', 'Brush.Tint.T3', 'secondary button text', 4.5],
  ['Brush.Status.SuccessText', 'Brush.Status.SuccessBg', 'success banner text', 4.5],
  ['Brush.Status.WarningText', 'Brush.Status.WarningBg', 'warning banner text', 4.5],
  ['Brush.Status.DangerText', 'Brush.Status.DangerBg', 'danger banner text', 4.5],
  ['Brush.Confidence.Excellent.Fg', 'Brush.Confidence.Excellent.Bg', 'confidence Excellent badge', 4.5],
  ['Brush.Confidence.Partial.Fg', 'Brush.Confidence.Partial.Bg', 'confidence Partial badge', 4.5],
  ['Brush.Confidence.Overwritten.Fg', 'Brush.Confidence.Overwritten.Bg', 'confidence Overwritten badge', 4.5],
  ['Brush.Confidence.MetadataOnly.Fg', 'Brush.Confidence.MetadataOnly.Bg', 'confidence Metadata badge', 4.5],
  ['Brush.Border.Focus', 'Brush.Bg.Window', 'FOCUS ring (state indicator)', 3.0, 'WCAG 1.4.11 — the component state identifier'],
  ['Brush.Border.Selected', 'Brush.Bg.Window', 'selection border (state indicator)', 3.0, 'WCAG 1.4.11'],
  ['Brush.Border.Default', 'Brush.Bg.Window', 'idle border (decorative)', 1.0, 'decorative: idle borders exempt (Fluent-style light chrome); identification via fill + focus ring'],
  ['Brush.Border.Strong', 'Brush.Bg.Window', 'idle strong border (decorative)', 1.0, 'decorative (as above)'],
  ['Brush.Border.Focus', 'Brush.Bg.Window', 'focus ring visibility', 3.0],
  ['Brush.Brand.Primary', 'Brush.Bg.Window', 'primary fill vs window (UI)', 3.0],
];

let failures = 0;
const themes = ['light', 'dark'];
const out = [];
for (const theme of themes) {
  out.push(`| Pair | Ratio | Min | Verdict | Note |`);
  out.push(`|---|---|---|---|---|`);
  for (const [fg, bg, role, min, note = ''] of pairs) {
    let f = get(fg, theme), b = get(bg, theme);
    if (!f || !b) { out.push(`| ${fg} / ${bg} | — | — | SYS | SystemColors-managed (HC) |`); continue; }
    // Alpha tokens composite over the theme base surface.
    if (hex8(f).length === 8 && parseInt(hex8(f).slice(0, 2), 16) < 255) f = blend(f, BASE[theme]);
    if (hex8(b).length === 8 && parseInt(hex8(b).slice(0, 2), 16) < 255) b = blend(b, BASE[theme]);
    const r = ratio(f, b);
    const verdict = r >= min ? (r >= 7 ? 'AAA' : r >= min ? 'AA' : 'FAIL') : 'FAIL';
    if (verdict === 'FAIL') { failures++; console.error('FAILPAIR', theme, fg, bg, r.toFixed(2), 'min', min); }
    out.push(`| ${fg.replace('Brush.', '')} / ${bg.replace('Brush.', '')} (${role}) | ${r.toFixed(2)}:1 | ${min}:1 | ${verdict} | ${note} |`);
  }
  out.push('');
}

const md = `# ACCESSIBILITY.md — Design-System Accessibility Evidence

> Generated by \`design/contrast-matrix.mjs\` from \`design/tokens.json\` (regenerate:
> \`node design/contrast-matrix.mjs > docs/ACCESSIBILITY.md\` — CI-run in design.yml).
> Method: WCAG 2.2 relative-luminance contrast; alpha tokens composited over the theme
> base surface before measurement; SystemColors (HC) entries are OS-managed and marked
> SYS (verified visually on the Windows runner instead). Failures break CI.

## 1. Contrast matrix — LIGHT theme

${out[0]}
${out.slice(1, out.indexOf('') + 1).join('\n')}

## 2. Contrast matrix — DARK theme

${out[out.indexOf('') + 1]}

## 3. Beyond color (WCAG 2.2)

- **1.4.1 Use of color**: status is never color-only — ConfidenceBadge carries text
  labels; health bands carry text + icon (Icon.Status.*); TRIM chip carries text;
  fit-verdict chips carry text. Verified in gallery.
- **2.4.7 Focus visible**: every interactive style sets \`FocusVisual.Primary\` (2px
  brand ring, 2px offset) — repairing DRW's \`outline:none\` gap (QSS L18). HC theme
  uses OS highlight-color ring.
- **2.5.5 Target size**: interactive min 28×28 (dialog buttons), standard 32×32,
  icon buttons 32×32 (DRW shipped 24 — raised, [ADV] QSS L1690).
- **2.3.3 Animation from interactions**: all motion collapses when
  \`SystemParameters.ClientAreaAnimation == false\` (Toast/ProgressSurface/gallery demos
  implement it; control templates animate only via those code paths).
- **4.1.2 Name/Role/Value**: custom controls set AutomationProperties.Name from their
  content (ToggleSwitch announces header + state; Banner/Toast announce severity +
  title + message; CircularGauge announces percent + paused).
- **1.4.4/1.4.10 Resize/reflow**: window min 800×600 with 3-pane collapsibility;
  WPF DIP layout reflows at 100–200% per-monitor DPI.

## 4. Screen-reader labels per component (register)

WizardStepper ("Step N of 3, label, current/done") · ConfidenceBadge (grade label) ·
HealthGauge (band caption) · ToggleSwitch (header + on/off) · Banner/Toast (severity +
title + message) · DriveCard (drive name + letter + capacity + badges as one name via
style) · SearchField (placeholder as name; clear button "Clear search") · Icon
(decorative by default — name only when informative) · FileTypeFilterChip (label +
count) · ProgressSurface (label + percent) · ContextMenu items (header text).
`;

fs.writeFileSync(path.join(ROOT, 'docs/ACCESSIBILITY.md'), md);
console.log(`Contrast matrix: ${pairs.length * 2} measurements across light/dark; failures: ${failures}`);
if (failures > 0) process.exit(1);
