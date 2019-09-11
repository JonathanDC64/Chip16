const fs = require('fs');
const path = require('path');


const filePath = 'instructions.txt';


const content = fs.readFileSync(filePath);

lines = (content.toString().split("\n"))

const data = new Object();

lines.forEach((line) => {
    const el = line.split(/\t+/);
    const code = parseInt(el[0], 16);
    const mnemonic = el[1].trim();
    const flags = (el[2] ? el[2].split("``") : []);

    data[code] = {
        opcode: code,
        mnemonic: mnemonic,
        flags: flags
    }
});

console.log(JSON.stringify(data, null, 2));