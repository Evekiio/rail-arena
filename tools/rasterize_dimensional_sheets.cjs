// Rasterize the native engineering-sheet SVGs. No generative image changes.
const fs = require('node:fs');
const path = require('node:path');
const sharp = require(process.argv[2] || 'sharp');
const folder = path.resolve('ArtSource/Original/ProductionBenchmarks-v1/Dimensional-References-v1');
(async () => {
  for (const name of ['RailRifle', 'LowWingCover']) {
    const source = path.join(folder, name + '-Six-View.svg');
    const output = path.join(folder, name + '-Six-View.png');
    await sharp(fs.readFileSync(source)).png().toFile(output);
    const info = await sharp(output).metadata();
    if (info.width !== 3900 || info.height !== 3200) throw Error('Sheet raster dimensions changed');
    console.log(name + ': 3900 x 3200 PNG');
  }
})().catch(error => { console.error(error.message); process.exitCode = 1; });
