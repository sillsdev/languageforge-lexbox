export function formatJsonForUi(json: object) {
  return JSON.stringify(json, null, 2)
    .split('\n') // Split into lines
    .slice(1, -1) // Remove the first and last line
    .map(line => line.slice(2)) // Remove one level of indentation
    .join('\n'); // Join the lines back together;
}
