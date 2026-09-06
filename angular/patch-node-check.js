const fs = require('fs');
const path = require('path');

const targetFile = path.join(__dirname, 'node_modules', '@angular', 'cli', 'src', 'utilities', 'node-version.js');

if (fs.existsSync(targetFile)) {
  let content = fs.readFileSync(targetFile, 'utf8');
  if (content.includes("'^22.22.3")) {
    content = content.replace("'^22.22.3", "'^22.18.0");
    fs.writeFileSync(targetFile, content, 'utf8');
    console.log('Successfully patched Angular CLI Node.js version check for v22.18.0 compatibility.');
  }
}
