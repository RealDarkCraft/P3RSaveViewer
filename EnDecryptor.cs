using System;

namespace P3RSaveViewer {
    // Based on https://github.com/illusion0001/P3R-Save-EnDecryptor
    public static class EnDecryptor {
        public const string KEY = "ae5zeitaix1joowooNgie3fahP5Ohph";

        public static byte[] Encrypyt(byte[] raw)
            => Transform(raw, Encrypt);

        public static byte[] Decrypt(byte[] raw)
            => Transform(raw, Decrypt);

        private static byte[] Transform(byte[] raw, Func<byte, byte, byte> fx) {
            var j = 0;
            for (var i = 0; i < raw.Length; i++) {
                if (j == KEY.Length)
                    j = 0;
                raw[i] = fx.Invoke(raw[i], (byte)KEY[j++]);
            }
            return raw;
        }

        private static byte Decrypt(byte data, byte key) {
            var bVar1 = data ^ key;
            return (byte) (bVar1 >> 4 & 3 | (bVar1 & 3) << 4 | bVar1 & 0xcc);
        }

        private static byte Encrypt(byte data, byte key)
            =>  (byte)((((data & 0xff) >> 4) & 3 | (data & 3) << 4 | data & 0xcc) ^ key);
    }
}