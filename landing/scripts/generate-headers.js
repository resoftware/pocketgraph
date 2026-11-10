#!/usr/bin/env node
/**
 * Security headers generator for Cloudflare Pages
 *
 * Runs after the build and writes <dist>/_headers with a Content-Security-Policy.
 * Inline scripts are allowed by their SHA-256 hash instead of 'unsafe-inline';
 * the hashes are recomputed from the built HTML on every build.
 *
 * Styles: when the HTML still has inline `style` attributes (which cannot be
 * hashed), the enforced policy allows 'unsafe-inline' for styles only, and a
 * Report-Only policy with the strict style rules shows in the browser console
 * what would break. Once the console is clean, set STRICT_STYLES to true.
 *
 * Fails the build on `on*="…"` attributes: the enforced CSP blocks them.
 */

import { createHash } from 'crypto';
import { readFileSync, readdirSync, statSync, writeFileSync } from 'fs';
import { join, relative } from 'path';

const DIST_DIR = process.argv[2] || 'dist';
const STRICT_STYLES = true;

// External origins the site actually uses
const ORIGINS = {
  // Rybbit analytics
  script: ['https://rybbit.resoftware.work'],
  style: ['https://fonts.googleapis.com'],
  font: ['https://fonts.gstatic.com'],
  img: [],
  media: [],
  connect: ['https://rybbit.resoftware.work'],
  frame: [],
  formAction: [],
};

function getHtmlFiles(dir) {
  const files = [];
  for (const entry of readdirSync(dir)) {
    const fullPath = join(dir, entry);
    if (statSync(fullPath).isDirectory()) {
      files.push(...getHtmlFiles(fullPath));
    } else if (entry.endsWith('.html')) {
      files.push(fullPath);
    }
  }
  return files;
}

function sha256(content) {
  return `'sha256-${createHash('sha256').update(content, 'utf8').digest('base64')}'`;
}

const scriptHashes = new Set();
const styleHashes = new Set();
const handlerViolations = [];
let styleAttributes = 0;

for (const file of getHtmlFiles(DIST_DIR)) {
  const html = readFileSync(file, 'utf8');

  for (const [, attrs, body] of html.matchAll(/<script\b([^>]*)>([\s\S]*?)<\/script>/gi)) {
    if (/\bsrc\s*=/i.test(attrs) || body.length === 0) continue;
    if (/type\s*=\s*["']?application\/(ld\+)?json/i.test(attrs)) continue;
    scriptHashes.add(sha256(body));
  }

  for (const [, body] of html.matchAll(/<style\b[^>]*>([\s\S]*?)<\/style>/gi)) {
    styleHashes.add(sha256(body));
  }

  styleAttributes += [...html.matchAll(/<[a-z][a-z0-9-]*\b[^>]*\sstyle\s*=\s*["']/gi)].length;

  for (const [match] of html.matchAll(/<[a-z][a-z0-9-]*\b[^>]*\son[a-z]+\s*=\s*["'][^>]*>/gi)) {
    handlerViolations.push(`${relative(DIST_DIR, file)}: ${match.slice(0, 120)}`);
  }
}

if (handlerViolations.length > 0) {
  console.error('Inline on* event handlers found; these are blocked by the CSP:');
  for (const violation of handlerViolations) console.error(`  ${violation}`);
  console.error('Use addEventListener in a <script> instead.');
  process.exit(1);
}

if (STRICT_STYLES && styleAttributes > 0) {
  console.error(`${styleAttributes} inline style attribute(s) found while STRICT_STYLES is on; use a class instead.`);
  process.exit(1);
}

const list = (...parts) => parts.flat().filter(Boolean).join(' ');
const strictStyle = list("style-src 'self'", ORIGINS.style, [...styleHashes].sort());
const looseStyle = list("style-src 'self'", ORIGINS.style, "'unsafe-inline'");

function policy(styleDirective) {
  return [
    "default-src 'self'",
    list("script-src 'self'", ORIGINS.script, [...scriptHashes].sort()),
    styleDirective,
    list("font-src 'self'", ORIGINS.font),
    list("img-src 'self' data:", ORIGINS.img),
    list("media-src 'self'", ORIGINS.media),
    list("connect-src 'self'", ORIGINS.connect),
    ORIGINS.frame.length ? list('frame-src', ORIGINS.frame) : "frame-src 'none'",
    "frame-ancestors 'none'",
    list("form-action 'self'", ORIGINS.formAction),
    "base-uri 'self'",
    "object-src 'none'",
    'upgrade-insecure-requests',
  ].join('; ');
}

const lines = [
  '/*',
  `  Content-Security-Policy: ${policy(STRICT_STYLES ? strictStyle : looseStyle)}`,
];
if (!STRICT_STYLES) {
  lines.push(`  Content-Security-Policy-Report-Only: ${policy(strictStyle)}`);
}
lines.push(
  '  X-Frame-Options: DENY',
  '  X-Content-Type-Options: nosniff',
  '  Referrer-Policy: strict-origin-when-cross-origin',
  '  Permissions-Policy: camera=(), microphone=(), geolocation=(), payment=(), usb=()',
  '',
);

writeFileSync(join(DIST_DIR, '_headers'), lines.join('\n'));
console.log(
  `_headers written: ${scriptHashes.size} script hash(es), ${styleHashes.size} style hash(es), ` +
    `${styleAttributes} inline style attribute(s)${STRICT_STYLES ? '' : ' (styles: unsafe-inline + Report-Only)'}`
);
