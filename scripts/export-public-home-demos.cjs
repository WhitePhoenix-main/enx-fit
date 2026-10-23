// Render editable demo artwork to transparent, high-resolution WebP assets.
// node scripts/export-public-home-demos.cjs [path-to-playwright] [path-to-sharp] [chrome-executable] [--native] [asset-name ...]
const fs = require('node:fs/promises');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const { chromium } = require(process.argv[2] || 'playwright');
const sharp = require(process.argv[3] || 'sharp');
const root = path.resolve(__dirname, '..');
const native = process.argv.includes('--native');
const sources = path.join(root, 'docs/references/public-home', native ? 'native-story' : 'demo-sources');
const output = path.join(root, 'enx-fit/wwwroot/images/landing/demos');

async function exportDemos() {
    const manifest = JSON.parse(await fs.readFile(path.join(sources, 'manifest.json'), 'utf8'));
    const browser = await chromium.launch({
        headless: true,
        ...(process.argv[4] ? { executablePath: process.argv[4] } : { channel: 'chrome' }),
        args: ['--allow-file-access-from-files']
    });
    try {
        const page = await browser.newPage({ deviceScaleFactor: 3, reducedMotion: 'reduce' });
        await fs.mkdir(output, { recursive: true });
        for (const asset of manifest) {
            const names = process.argv.slice(5).filter(name => name !== '--native');
            if (names.length && !names.some(name => asset.file === `${name}.webp` || asset.file.startsWith(`${name}-`))) continue;
            const file = asset.file.replace(/\.webp$/, '.html');
            await page.setViewportSize({ width: file.includes('-mobile') ? (native ? 480 : 390) : (native ? 1280 : 1440), height: 1600 });
            await page.goto(pathToFileURL(path.join(sources, file)).href, { waitUntil: 'networkidle' });
            await page.evaluate(() => document.fonts.ready);
            const stage = page.locator(native ? '.native-stage' : '.export-stage');
            const bounds = await stage.boundingBox();
            const image = await stage.screenshot({ omitBackground: true });
            const result = await sharp(image).webp({ quality: 94, alphaQuality: 100, effort: 6 }).toFile(path.join(output, asset.file));
            asset.pixelWidth = result.width;
            asset.pixelHeight = result.height;
            asset.width = Math.round(bounds.width);
            asset.height = Math.round(bounds.height);
            asset.bytes = result.size;
            console.log(`${asset.file}: ${result.width} × ${result.height}, ${result.size} bytes`);
        }
        await fs.writeFile(path.join(sources, 'manifest.json'), JSON.stringify(manifest, null, 2) + '\n');
    } finally {
        await browser.close();
    }
}
exportDemos().catch(error => { console.error(error); process.exitCode = 1; });
