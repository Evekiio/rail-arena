const fs = require('node:fs');
const path = require('node:path');
const sharp = require(process.argv[2] || 'sharp');
const folder = path.resolve(process.argv[3] || 'ArtSource/Original/RailRifle-Modeling-References-v2');
const sheets = ['RailRifle-Six-View','02-Stock-Construction','03-Axial-Contacts','04-Exploded-Assembly','05-Section-Anchors','06-Materials-Motion'];
(async () => {
 for (const name of sheets) {
  const output = path.join(folder,name+'.png');
  await sharp(fs.readFileSync(path.join(folder,name+'.svg'))).png().toFile(output);
  const info = await sharp(output).metadata();
  if (info.width !== 3900 || info.height !== 3200) throw Error('Wrong board size');
  console.log(name + ' exported');
 }
})().catch(error => {console.error(error.message);process.exitCode=1;});
