const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const { join } = require('node:path');
const test = require('node:test');

const manifest = JSON.parse(
    readFileSync(join(__dirname, '..', 'package.json'), 'utf8')
);

function versionParts(versionRange) {
    const match = versionRange.match(/(\d+)\.(\d+)\.(\d+)/);
    assert.ok(match, `Expected a semantic version in "${versionRange}"`);
    return match.slice(1).map(Number);
}

test('@types/vscode does not exceed the minimum supported VS Code version', () => {
    const engineVersion = versionParts(manifest.engines.vscode);
    const typesVersion = versionParts(manifest.devDependencies['@types/vscode']);
    const comparison = typesVersion.findIndex(
        (part, index) => part !== engineVersion[index]
    );

    assert.ok(
        comparison === -1 || typesVersion[comparison] < engineVersion[comparison],
        `@types/vscode ${manifest.devDependencies['@types/vscode']} exceeds engines.vscode ${manifest.engines.vscode}`
    );
});
