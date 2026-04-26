#!/usr/bin/env node
// t1k-origin: kit=theonekit-core | repo=The1Studio/theonekit-core | module=null | protected=true
// project-detector.cjs — SessionStart hook: auto-detect project type and framework
'use strict';
try {
  const fs = require('fs');
  const path = require('path');
  const cwd = process.cwd();

  function exists(p) { return fs.existsSync(path.join(cwd, p)); }
  function readJson(p) { try { return JSON.parse(fs.readFileSync(path.join(cwd, p), 'utf8')); } catch { return null; } }

  let projectType = 'unknown', framework = '', packageManager = '';

  // TheOneKit project
  if (exists('.claude/metadata.json')) {
    const meta = readJson('.claude/metadata.json');
    if (meta) {
      projectType = 'theonekit';
      // Detect framework from multiple metadata formats: name field, kits key, or kitName
      framework = meta.kitName || (meta.kits ? Object.keys(meta.kits)[0] : null) || meta.name || 'core';
    }
  }
  // Unity
  else if (exists('Assets') && exists('ProjectSettings')) { projectType = 'unity'; framework = 'Unity'; }
  // Cocos
  else if (exists('assets') && (exists('project.json') || exists('settings/project.json'))) { projectType = 'cocos'; framework = 'Cocos Creator'; }
  // React Native
  else if (exists('app.json') && (exists('metro.config.js') || exists('metro.config.cjs'))) { projectType = 'react-native'; framework = exists('.expo') ? 'Expo' : 'React Native CLI'; }
  // Node.js
  else if (exists('package.json')) {
    projectType = 'node';
    const pkg = readJson('package.json');
    if (pkg) {
      const deps = { ...pkg.dependencies, ...pkg.devDependencies };
      if (deps['next']) framework = 'Next.js';
      else if (deps['nuxt']) framework = 'Nuxt';
      else if (deps['@nestjs/core']) framework = 'NestJS';
      else if (deps['express']) framework = 'Express';
      else if (deps['react']) framework = 'React';
      else if (deps['vue']) framework = 'Vue';
    }
    packageManager = exists('pnpm-lock.yaml') ? 'pnpm' : exists('yarn.lock') ? 'yarn' : 'npm';
  }
  // Python
  else if (exists('pyproject.toml') || exists('requirements.txt')) {
    projectType = 'python';
    if (exists('manage.py')) framework = 'Django';
  }
  // Go
  else if (exists('go.mod')) { projectType = 'go'; }
  // Rust
  else if (exists('Cargo.toml')) { projectType = 'rust'; }
  // .NET
  else if (fs.readdirSync(cwd).some(f => f.endsWith('.csproj') || f.endsWith('.sln'))) { projectType = 'dotnet'; }
  // Docker
  else if (exists('Dockerfile') || exists('docker-compose.yml')) { projectType = 'containerized'; }
  // Configuration-only (like theonekit-core without metadata)
  else if (exists('.claude') && !exists('src') && !exists('package.json')) { projectType = 'configuration'; }

  const parts = [`[project-type] ${projectType}`];
  if (framework) parts.push(`[framework] ${framework}`);
  if (packageManager) parts.push(`[package-manager] ${packageManager}`);
  console.log(parts.join(' | '));

  process.exit(0);
} catch (e) {
  process.exit(0); // fail-open
}
