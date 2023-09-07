export async function hash(password: string): Promise<string> {
    const msgUint8 = new TextEncoder().encode(password) // encode as (utf-8) Uint8Array
    let hashBuffer: ArrayBuffer;
    const c = typeof crypto !== 'undefined' ? crypto : await import('node:crypto');
    if (c && c.subtle) {
        hashBuffer = await c.subtle.digest('SHA-1', msgUint8) // hash the message
    } else {
        console.log('crypto.subtle not found; cryptop module was', c);
        throw new Error('crypto.subtle not found -- are we running on an old version of Node?');
    }
    const hashArray = Array.from(new Uint8Array(hashBuffer)) // convert buffer to byte array
    const hashHex = hashArray.map(b => b.toString(16).padStart(2, '0')).join('') // convert bytes to hex string

    return hashHex
}
