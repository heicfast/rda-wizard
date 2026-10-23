#!/usr/bin/env node
// design/generate-tokens.mjs — generate XAML resource dictionaries + C# metrics from tokens.json
// THE SINGLE SOURCE OF TRUTH IS tokens.json. Generated files are committed (byte-stable) so
// the app builds without Node; CI re-runs the generator and diffs (design-tokens job) to
// catch hand-edits and drift.
//
// Outputs (all under src/RdaWizard.Design/):
//   Themes/Tokens.xaml            — theme-independent geometry/typography/motion
//   Themes/Theme.Light.xaml       — colors/brushes/gradients/effects (light)
//   Themes/Theme.Dark.xaml        — (dark)
//   Themes/Theme.HighContrast.xaml— (HC; @-prefixed values become SystemColors DynamicResources)
//   Metrics.cs                    — C# constants for geometry (VM-consumable numbers)
//
// Usage: node design/generate-tokens.mjs [--check]
//   --check: regenerate to memory, compare against on-disk files, exit 1 on drift.
import fs from 'node:fs';
import path from 'node:path';

const ROOT = path.resolve(path.dirname(process.argv[1]), '..');
const T = JSON.parse(fs.readFileSync(path.join(ROOT, 'design/tokens.json'), 'utf8'));

// ---------- helpers ----------
const XAML_HDR = (title) => `<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:sys="clr-namespace:System;assembly=mscorlib">
    <!--
      ${title}
      GENERATED FILE — do not edit by hand. Source of truth: design/tokens.json
      (regenerate: node design/generate-tokens.mjs). Tokens v${T.version}.
      Evidence keys (QSS/TDI/VE/QE/ADV) are recorded per-key in design/tokens.json;
      see docs/VISUAL-SPEC.md for the authority order and rationale.
    -->
`;

// WPF SystemColors mapping for HC dynamic brushes (OS-remapped in real high-contrast themes)
const SYSBRUSH = {
  '@WindowColor': '{x:Static SystemColors.WindowBrushKey}',
  '@WindowTextColor': '{x:Static SystemColors.WindowTextBrushKey}',
  '@HighlightColor': '{x:Static SystemColors.HighlightBrushKey}',
  '@HighlightTextColor': '{x:Static SystemColors.HighlightTextBrushKey}',
  '@HotTrackColor': '{x:Static SystemColors.HotTrackBrushKey}',
  '@GrayTextColor': '{x:Static SystemColors.GrayTextBrushKey}',
  '@WindowFrameColor': '{x:Static SystemColors.ActiveBorderBrushKey}',
  '@InfoBackgroundColor': '{x:Static SystemColors.InfoBrushKey}',
};

const normHex = (v) => {
  if (typeof v !== 'string') throw new Error('bad hex: ' + v);
  let h = v.replace('#', '').toUpperCase();
  if (h.length === 6) h = 'FF' + h;
  if (h.length !== 8) throw new Error('bad hex length: ' + v);
  return '#' + h;
};

function solidBrush(key, value, indent = '    ') {
  if (value.startsWith('@')) {
    const res = SYSBRUSH[value];
    if (!res) throw new Error('unknown SystemColors key: ' + value);
    return `${indent}<SolidColorBrush x:Key="${key}" Color="{DynamicResource ${res}}" />`;
  }
  return `${indent}<SolidColorBrush x:Key="${key}" Color="${normHex(value)}" />`;
}

function gradientBrush(g, theme, indent = '    ') {
  const stops = g.stops[theme];
  if (!stops) { // hc: fall back to solid from first light stop
    return `${indent}<SolidColorBrush x:Key="${g.key}" Color="${normHex(g.stops.light[0])}" />`;
  }
  const angle = g.angle || 0;
  // angle 0 = horizontal (→), 90 = vertical (↓). QSS x1..x2/y1..y2 mapping.
  const sp = angle === 90 ? '0,0' : '0,0';
  const ep = angle === 90 ? '0,1' : '1,0';
  const offsets = g.offsets || stops.map((_, i) => i / (stops.length - 1));
  const stopEls = stops.map((s, i) =>
    `${indent}    <GradientStop Color="${normHex(s)}" Offset="${offsets[i]}" />`).join('\n');
  return `${indent}<LinearGradientBrush x:Key="${g.key}" StartPoint="${sp}" EndPoint="${ep}">
${stopEls}
${indent}</LinearGradientBrush>`;
}

// ---------- color sections ----------
const SECTION_TITLES = {
  brand: 'Brand / interaction',
  status: 'Status semantics',
  bg: 'Backgrounds',
  text: 'Text',
  border: 'Borders & dividers',
  tint: 'Blue tint ladder (hover/selected fills)',
  disabled: 'Disabled states',
  confidence: 'Confidence badges (recovery-chance language)',
  health: 'Health-gauge bands',
  chart: 'Chart accents (8 result categories)',
  misc: 'Misc',
};

function themeFile(theme, themeTitle) {
  let out = XAML_HDR(themeTitle);
  for (const [group, tokens] of Object.entries(T.colors)) {
    out += `\n    <!-- ===== ${SECTION_TITLES[group] ?? group} ===== -->\n`;
    for (const t of tokens) {
      out += solidBrush(t.key, t[theme]) + '\n';
    }
  }
  out += `\n    <!-- ===== Gradients ===== -->\n`;
  for (const g of T.gradients) out += gradientBrush(g, theme) + '\n';
  const shadowHex = normHex(T.colors.misc.find(t => t.key === 'Brush.Shadow.Hue')[theme]);
  out += `\n    <!-- ===== Elevation (DropShadowEffects) ===== -->\n`;
  for (const e of T.effects) {
    const p = e[theme];
    if (p.blur === 0 && p.depth === 0 && p.opacity === 0) {
      out += `    <DropShadowEffect x:Key="${e.key}" BlurRadius="0" ShadowDepth="0" Opacity="0" Color="${shadowHex}" />\n`;
    } else {
      out += `    <DropShadowEffect x:Key="${e.key}" BlurRadius="${p.blur}" ShadowDepth="${p.depth}" Direction="270"\n                      Opacity="${p.opacity}" Color="${shadowHex}" />\n`;
    }
  }
  out += '</ResourceDictionary>\n';
  return out;
}

// ---------- theme-independent tokens ----------
function tokensFile() {
  const G = T.geometry, TY = T.typography, M = T.motion;
  let out = XAML_HDR('Design tokens: sizes, spacing, radii, typography, motion — theme-INDEPENDENT.');
  const thick = (k, v) => `    <Thickness x:Key="${k}">${v}</Thickness>`;
  const dbl = (k, v) => `    <sys:Double x:Key="${k}">${v}</sys:Double>`;
  const cr = (k, v) => v === 999 ? `    <CornerRadius x:Key="${k}">999</CornerRadius>` : `    <CornerRadius x:Key="${k}">${v}</CornerRadius>`;

  out += `\n    <!-- ===== Spacing (4px base grid) — Thickness so Padding/Margin consume directly ===== -->\n`;
  for (const [k, v] of Object.entries(G.space)) out += thick(k, v) + '\n';
  out += `\n    <!-- ===== Control heights ===== -->\n`;
  for (const [k, v] of Object.entries(G.heights)) out += dbl(k, v) + '\n';
  out += `\n    <!-- ===== Pane/control widths ===== -->\n`;
  for (const [k, v] of Object.entries(G.widths)) out += dbl(k, v) + '\n';
  out += `\n    <!-- ===== Corner radii ===== -->\n`;
  for (const [k, v] of Object.entries(G.radius)) out += cr(k, v) + '\n';
  out += `\n    <!-- ===== Icon sizes ===== -->\n`;
  for (const [k, v] of Object.entries(G.icons)) out += dbl(k, v) + '\n';
  out += `\n    <!-- ===== Window & page metrics ===== -->\n`;
  for (const [k, v] of Object.entries(G.window)) out += dbl(k, v) + '\n';
  out += `\n    <!-- ===== Stroke widths ===== -->\n`;
  for (const [k, v] of Object.entries(G.strokes)) out += dbl(k, v) + '\n';
  out += `\n    <!-- ===== Table columns ===== -->\n`;
  for (const [k, v] of Object.entries(G.columns)) out += dbl(k, v) + '\n';
  out += `\n    <!-- ===== Typography ramp (sizes in WPF DIPs; families with CJK fallbacks) ===== -->\n`;
  for (const [k, v] of Object.entries(TY.sizes)) out += dbl(k, v) + '\n';
  for (const [k, v] of Object.entries(TY.families)) out += `    <FontFamily x:Key="${k}">${v}</FontFamily>\n`;
  out += `\n    <!-- ===== Motion durations (ms) — reduce-motion collapses to 0 ===== -->\n`;
  for (const [k, v] of Object.entries(M.durations)) out += dbl(k, v) + '\n';
  out += '</ResourceDictionary>\n';
  return out;
}

// ---------- C# metrics ----------
function metricsFile() {
  const G = T.geometry;
  const ident = (k) => k.replace(/[.\-]/g, '_');
  const constLine = (k, v, comment) => `        public const double ${ident(k)} = ${v};${comment ? ' // ' + comment : ''}`;
  let out = `// <auto-generated FROM="design/tokens.json" generator="design/generate-tokens.mjs">
// GENERATED FILE — do not edit. Geometry metrics for ViewModel consumption.
// Colors/brushes live ONLY in XAML resource dictionaries (DynamicResource keys).
#nullable enable
namespace RdaWizard.Design
{
    /// <summary>Numeric design metrics (theme-independent geometry), generated from tokens.json.</summary>
    public static class Metrics
    {
`;
  const groups = [
    ['Spacing', G.space], ['Heights', G.heights], ['Widths', G.widths],
    ['Radii', G.radius], ['Icons', G.icons], ['Window', G.window],
    ['Strokes', G.strokes], ['Columns', G.columns],
  ];
  for (const [name, obj] of groups) {
    out += `        // ${name}\n`;
    for (const [k, v] of Object.entries(obj)) out += constLine(k, v) + '\n';
    out += '\n';
  }
  out += `        // Motion durations (ms)\n`;
  for (const [k, v] of Object.entries(T.motion.durations)) out += constLine(k, v) + '\n';
  out += `    }
}
`;
  return out;
}

// ---------- parity check ----------
function keySet(theme) {
  const s = new Set();
  for (const tokens of Object.values(T.colors)) for (const t of tokens) s.add(t.key);
  for (const g of T.gradients) s.add(g.key);
  for (const e of T.effects) s.add(e.key);
  return s;
}
const kl = keySet('light'), kd = keySet('dark'), kh = keySet('hc');
if (kl.size !== kd.size || kl.size !== kh.size) {
  const missing = [...new Set([...kl].filter(k => !kd.has(k) || !kh.has(k)))];
  throw new Error('THEME PARITY VIOLATION: ' + missing.join(', '));
}

// ---------- write ----------
const outputs = {
  'src/RdaWizard.Design/Themes/Tokens.xaml': tokensFile(),
  'src/RdaWizard.Design/Themes/Theme.Light.xaml': themeFile('light', 'LIGHT theme — generated from tokens.json (v' + T.version + ').\n      Every key exists in Theme.Dark.xaml and Theme.HighContrast.xaml — key-set parity is\n      enforced by the generator and CI-checked. Consumers reference via DynamicResource only.'),
  'src/RdaWizard.Design/Themes/Theme.Dark.xaml': themeFile('dark', 'DARK theme — generated from tokens.json (v' + T.version + ').\n      Exact key counterparts of Theme.Light.xaml / Theme.HighContrast.xaml.\n      Dark chrome is border-driven (shadows near-invisible on #1B1D1F — DESIGN-SYSTEM §5.3).'),
  'src/RdaWizard.Design/Themes/Theme.HighContrast.xaml': themeFile('hc', 'HIGH-CONTRAST theme — generated from tokens.json (v' + T.version + ').\n      @-tokens resolve to WPF SystemColors DynamicResources so real Windows HC themes\n      remap colors at the OS level. Semantic statuses use high-visibility literals.\n      Elevation effects are OFF (flat + border depth). Key parity with Light/Dark enforced.'),
  'src/RdaWizard.Design/Metrics.cs': metricsFile(),
};

const args = process.argv.slice(2);
if (args.includes('--check')) {
  let drift = false;
  for (const [rel, content] of Object.entries(outputs)) {
    const disk = fs.readFileSync(path.join(ROOT, rel), 'utf8');
    if (disk !== content) { console.error('DRIFT: ' + rel); drift = true; }
  }
  if (drift) { console.error('Generated files differ from tokens.json — regenerate and commit.'); process.exit(1); }
  console.log(`design-tokens check OK: ${Object.keys(outputs).length} files byte-stable, ${kl.size} theme keys in parity (light/dark/hc).`);
} else {
  for (const [rel, content] of Object.entries(outputs)) {
    fs.writeFileSync(path.join(ROOT, rel), content);
    console.log('wrote', rel, `(${content.split('\n').length} lines)`);
  }
  console.log(`Theme key parity: ${kl.size} keys in each of light/dark/hc.`);
}
