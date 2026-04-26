#!/usr/bin/env node
// t1k-origin: kit=theonekit-core | repo=The1Studio/theonekit-core | module=null | protected=true
'use strict';

/**
 * ANSI Terminal Colors - Cross-platform color support for statusline
 * Supports NO_COLOR, FORCE_COLOR auto-detection
 * T1K-native — no external dependencies
 */

const RESET = '\x1b[0m';
const DIM = '\x1b[2m';
const RED = '\x1b[31m';
const GREEN = '\x1b[32m';
const YELLOW = '\x1b[33m';
const MAGENTA = '\x1b[35m';
const CYAN = '\x1b[36m';

const shouldUseColor = (() => {
  if (process.env.NO_COLOR) return false;
  if (process.env.FORCE_COLOR) return true;
  return true; // Default true for statusline (Claude Code handles TTY display)
})();

let _colorOverride = null;

function setColorEnabled(enabled) { _colorOverride = enabled; }

function isColorEnabled() {
  if (process.env.NO_COLOR) return false;
  if (_colorOverride !== null) return _colorOverride;
  return shouldUseColor;
}

function colorize(text, code) {
  if (!isColorEnabled()) return String(text);
  return `${code}${text}${RESET}`;
}

function green(text) { return colorize(text, GREEN); }
function yellow(text) { return colorize(text, YELLOW); }
function red(text) { return colorize(text, RED); }
function cyan(text) { return colorize(text, CYAN); }
function magenta(text) { return colorize(text, MAGENTA); }
function dim(text) { return colorize(text, DIM); }

function getContextColor(percent) {
  if (percent >= 85) return RED;
  if (percent >= 70) return YELLOW;
  return GREEN;
}

function coloredBar(percent, width = 12) {
  const clamped = Math.max(0, Math.min(100, percent));
  const filled = Math.round((clamped / 100) * width);
  const empty = width - filled;
  if (!isColorEnabled()) return '▰'.repeat(filled) + '▱'.repeat(empty);
  const color = getContextColor(percent);
  return `${color}${'▰'.repeat(filled)}${DIM}${'▱'.repeat(empty)}${RESET}`;
}

module.exports = { RESET, green, yellow, red, cyan, magenta, dim, getContextColor, coloredBar, setColorEnabled, isColorEnabled };
