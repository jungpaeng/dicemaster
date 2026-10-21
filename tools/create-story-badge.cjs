// Development only: NODE_PATH=<directory containing sharp> node tools/create-story-badge.cjs
// Normal builds use the checked-in image. No image decoder/new Unity module at runtime.
const fs = require('fs');
const path = require('path');
const sharp = require('sharp');
(async () => {
  const directory = path.join(__dirname, '../src/DiceMaster/Resources');
  const pipeline = sharp(path.join(directory, 'StoryBadge.svg'), { density: 288 }).resize(64, 64);
  await pipeline.clone().png().toFile(path.join(directory, 'StoryBadge.png'));
  const rgba = await pipeline.clone().ensureAlpha().raw().toBuffer();
  if (rgba.length !== 64 * 64 * 4) throw new Error('Unexpected badge image shape');
  const bottomUp = Buffer.alloc(rgba.length);
  for (let y = 0; y < 64; y++) rgba.copy(bottomUp, (63 - y) * 256, y * 256, (y + 1) * 256);
  fs.writeFileSync(path.join(directory, 'StoryBadge.rgba'), bottomUp);
})().catch(error => { console.error(error); process.exitCode = 1; });
