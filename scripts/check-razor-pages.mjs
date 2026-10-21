import fs from 'node:fs';
import path from 'node:path';

const root = path.resolve('enx-fit');
const categories = new Set(['Public', 'Account', 'Dashboard', 'Training', 'Progress', 'Coaching', 'Nutrition', 'Admin', 'Errors']);
const views = [];
const errors = [];
function visit(directory) {
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const file = path.join(directory, entry.name);
    if (entry.isDirectory()) visit(file);
    else if (entry.name.endsWith('.cshtml')) views.push({ file, source: fs.readFileSync(file, 'utf8') });
  }
}
visit(path.join(root, 'Pages'));
visit(path.join(root, 'Areas'));
const pages = views.filter(view => /^@page\b/m.test(view.source)).map(view => {
  const relative = path.relative(root, view.file).replaceAll(path.sep, '/');
  const area = relative.startsWith('Areas/') ? relative.split('/')[1] : '';
  const name = '/' + relative.split('/Pages/').pop().replace(/^Pages\//, '').slice(0, -7);
  if (!area && !categories.has(name.split('/')[1]))
    errors.push(relative + ': page must belong to a semantic category');
  return { ...view, area, name };
});
const identities = new Set(pages.map(page => page.area + ':' + page.name));
let references = 0;
for (const view of views) {
  const relative = path.relative(root, view.file).replaceAll(path.sep, '/');
  const ambientArea = relative.startsWith('Areas/') ? relative.split('/')[1] : '';
  for (const match of view.source.matchAll(/asp-page="(\/[^"@]+)"/g)) {
    const tagStart = view.source.lastIndexOf('<', match.index);
    const tagEnd = view.source.indexOf('>', match.index);
    const tag = view.source.slice(tagStart, tagEnd);
    const areaAttribute = tag.match(/asp-area="([^"]*)"/);
    if (areaAttribute?.[1].includes('@')) continue;
    const area = areaAttribute?.[1] ?? ambientArea;
    references++;
    if (!identities.has(area + ':' + match[1]) &&
        !(relative.includes('/Shared/') && pages.some(page => page.name === match[1])))
      errors.push(relative + ': unresolved asp-page ' + area + ':' + match[1]);
  }
  for (const match of view.source.matchAll(/["'](~?\/(?:Pages|Areas)\/[^"']+\.cshtml)["']/g)) {
    references++;
    if (!fs.existsSync(path.join(root, match[1].replace(/^~?\//, ''))))
      errors.push(relative + ': unresolved view ' + match[1]);
  }
}
if (errors.length) {
  console.error(errors.join('\n'));
  process.exitCode = 1;
} else {
  console.log('Checked ' + pages.length + ' categorized/Identity pages and ' + references + ' literal page/view references.');
}
