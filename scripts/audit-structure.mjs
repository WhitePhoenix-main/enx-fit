import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';

const root = path.resolve('enx-fit');
const files = [];
const ignored = new Set(['bin', 'obj', 'Migrations', 'lib', 'vendor', 'images', 'fonts', 'App_Data']);
function visit(dir) {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    if (ignored.has(entry.name)) continue;
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) visit(full);
    else if (/\.(cs|cshtml|js|css)$/.test(entry.name)) files.push(full);
  }
}
visit(root);
const exact = new Map(), windows = new Map();
for (const file of files) {
  const relative = path.relative(root, file).replaceAll('\\', '/');
  const source = fs.readFileSync(file, 'utf8');
  const hash = crypto.createHash('sha256').update(source.trim().replaceAll('\r\n', '\n')).digest('hex');
  exact.set(hash, [...(exact.get(hash) ?? []), relative]);
  if (!/\.(cs|cshtml)$/.test(file)) continue;
  const lines = source.split(/\r?\n/).map((text, i) => ({ text: text.trim(), line: i + 1 }))
    .filter(x => x.text && !/^(using |@using |[{}]|break;|\/\/)/.test(x.text));
  for (let i = 0; i <= lines.length - 8; i++) {
    const block = lines.slice(i, i + 8).map(x => x.text).join('\n');
    if (block.length < 220) continue;
    const hash = crypto.createHash('sha256').update(block).digest('hex');
    windows.set(hash, [...(windows.get(hash) ?? []), { file: relative, line: lines[i].line }]);
  }
}
const pairs = new Map();
for (const occurrences of windows.values()) {
  for (let i = 0; i < occurrences.length; i++)
    for (let j = i + 1; j < occurrences.length; j++) {
      const a = occurrences[i], b = occurrences[j];
      if (a.file === b.file) continue;
      const key = [a.file, b.file].sort().join(' | ');
      const item = pairs.get(key) ?? { files: key, repeatedWindows: 0, example: [a.line, b.line] };
      item.repeatedWindows++;
      pairs.set(key, item);
    }
}
console.log(JSON.stringify({
  files: files.length,
  exactDuplicates: [...exact.values()].filter(group => group.length > 1),
  repeatedBlocks: [...pairs.values()].sort((a, b) => b.repeatedWindows - a.repeatedWindows).slice(0, 22)
}, null, 2));
