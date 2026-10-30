// Export the installed Lucide library faithfully; never approximate icons with text glyphs.
const fs = require('fs');
const path = require('path');
const modules = process.argv[2];
if (!modules) throw new Error('Usage: node tools/build_ui_brand_assets.cjs <node_modules directory>');
const lucide = require(path.join(modules, 'lucide'));
const sharp = require(path.join(modules, 'sharp'));
const root = path.resolve(__dirname, '..');
const source = path.join(root, 'ArtSource/Original/UI-Brand-v1/Icons');
const output = path.join(root, 'RailArena/Assets/Resources/UI/Brand/Icons');
const license = path.join(root, 'RailArena/Assets/ThirdParty/Lucide');
for (const p of [source, output, license]) fs.mkdirSync(p, { recursive: true });
const icons = {
  health: 'HeartPulse', rail: 'Zap', score: 'Trophy', clock: 'Clock', target: 'Target', duel: 'Swords',
  settings: 'Settings2', audio: 'Volume2', display: 'Monitor', aim: 'Crosshair', contender: 'UserRound',
  walk: 'Footprints', sprint: 'Gauge', crouch: 'ArrowDownToLine', play: 'Play', next: 'ChevronRight',
  back: 'ArrowLeft', controls: 'Keyboard', help: 'CircleHelp', quit: 'LogOut', network: 'Wifi',
  alert: 'TriangleAlert', map: 'MapPin', check: 'Check', comfort: 'Eye', restart: 'RotateCcw',
  pause: 'Pause', unlimited: 'Infinity', shield: 'ShieldCheck', tuning: 'SlidersHorizontal'
};
async function main() {
  for (const [name, upstream] of Object.entries(icons)) {
    const nodes = lucide.icons[upstream];
    if (!nodes) throw new Error('Missing upstream icon ' + upstream);
    const content = nodes.map(([tag, attributes]) => '<' + tag + ' ' + Object.entries(attributes)
      .map(([key, value]) => key + '="' + value + '"').join(' ') + '/>').join('');
    const svg = '<svg xmlns="http://www.w3.org/2000/svg" width="96" height="96" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round">' + content + '</svg>';
    fs.writeFileSync(path.join(source, name + '.svg'), svg);
    await sharp(Buffer.from(svg)).png().toFile(path.join(output, name + '.png'));
  }
  fs.copyFileSync(path.join(modules, 'lucide/LICENSE'), path.join(license, 'LICENSE.txt'));
  fs.writeFileSync(path.join(root, 'ArtSource/Original/UI-Brand-v1/Icons.json'), JSON.stringify({
    library: 'Lucide', version: require(path.join(modules, 'lucide/package.json')).version,
    license: 'ISC; upstream Feather notices retained', size: 96, stroke: 1.75, icons
  }, null, 2));
  console.log('Exported ' + Object.keys(icons).length + ' licensed icons.');
}
main().catch(error => { console.error(error); process.exitCode = 1; });
