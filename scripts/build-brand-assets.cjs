// Export the canonical EF vector to browser and installable-app icons.
// node scripts/build-brand-assets.cjs [path-to-sharp]
const fs = require('node:fs/promises');
const path = require('node:path');
const sharp = require(process.argv[2] || 'sharp');
const root = path.resolve(__dirname, '../enx-fit/wwwroot');

async function build() {
    const source = await fs.readFile(path.join(root, 'images/brand/ef-mark.svg'), 'utf8');
    const mark = source.match(/<g id="enix-ef">([\s\S]*?)<\/g>/)[1];
    for (const [plan, color, suffix] of [['free', '#4389ff', ''], ['pro', '#ffad52', '-pro'], ['coach', '#ff4668', '-coach']]) {
        const icon = scale => `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128"><rect width="128" height="128" rx="27" fill="#111518"/><g transform="translate(${(128 - 124 * scale) / 2} ${(128 - 72 * scale) / 2}) scale(${scale})" fill="${color}">${mark}</g></svg>\n`;
        await fs.writeFile(path.join(root, `images/brand/icon-${plan}.svg`), icon(.8));
        for (const size of [180, 192, 512]) {
            await sharp(Buffer.from(icon(.7))).resize(size, size).png().toFile(path.join(root, `images/brand/icon-${size}${suffix}.png`));
        }
        if (plan === 'free') await fs.writeFile(path.join(root, 'images/landing/favicon.svg'), icon(.8));
        const manifest = {
            id: '/Dashboard', name: 'Enix Fit', short_name: 'Enix Fit', start_url: '/Dashboard', scope: '/',
            display: 'standalone', background_color: '#111518', theme_color: '#111518', lang: 'ru',
            icons: [192, 512].map(size => ({ src: `/images/brand/icon-${size}${suffix}.png`, sizes: `${size}x${size}`, type: 'image/png', purpose: 'any maskable' }))
                .concat([{ src: `/images/brand/icon-${plan}.svg`, sizes: 'any', type: 'image/svg+xml', purpose: 'any' }])
        };
        await fs.writeFile(path.join(root, `manifest${suffix}.webmanifest`), JSON.stringify(manifest, null, 2) + '\n');
    }
    // ICO supports PNG payloads; keep the legacy browser fallback on the same EF mark.
    const sizes = [16, 32, 48, 256];
    const pngs = await Promise.all(sizes.map(size => sharp(path.join(root, 'images/brand/icon-free.svg')).resize(size, size).png().toBuffer()));
    const directory = Buffer.alloc(6 + sizes.length * 16);
    directory.writeUInt16LE(1, 2); directory.writeUInt16LE(sizes.length, 4);
    let offset = directory.length;
    sizes.forEach((size, index) => {
        const entry = 6 + index * 16;
        directory[entry] = directory[entry + 1] = size === 256 ? 0 : size;
        directory.writeUInt16LE(1, entry + 4); directory.writeUInt16LE(32, entry + 6);
        directory.writeUInt32LE(pngs[index].length, entry + 8); directory.writeUInt32LE(offset, entry + 12);
        offset += pngs[index].length;
    });
    await fs.writeFile(path.join(root, 'favicon.ico'), Buffer.concat([directory, ...pngs]));
}
build().catch(error => { console.error(error); process.exitCode = 1; });
