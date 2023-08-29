import { promises as fsp } from 'fs';

const file = process.argv[2] || "eg.txt";

const input = (await fsp.readFile(file, { encoding: 'utf-8' }))
    .split(/\r?\n/)
    .map(line => parseInt(line, 10));

function* window(depths: number[], size = 3) {
    while (depths.length >= size) {
        yield depths.slice(0, size);
        depths = depths.slice(1);
    }
}

function increases(depths: number[]) {
    return depths
        .reduce(
            (acc, depth) => ({
                previous: depth,
                increases: depth > acc.previous ? acc.increases + 1 : acc.increases,
            }),
            { previous: Number.POSITIVE_INFINITY, increases: 0 },
        )
        .increases;
}

const answer1 = increases(input);

console.log(`there are ${answer1} depths greater than the preceding depth`);

const windowed = [...window(input)] // you seriously can't map over a generator?! :(
    .map((depths) => depths.reduce((acc, depth) => acc + depth));

const answer2 = increases(windowed);

console.log(`there are ${answer2} windows with a depth sum greater than the preceding window`);
