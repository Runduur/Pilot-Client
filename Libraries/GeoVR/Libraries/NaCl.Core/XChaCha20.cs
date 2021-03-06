namespace NaCl.Core
{
    using System;
    using System.Security.Cryptography;

    using Base;
    using Internal;

    /// <summary>
    /// A stream cipher based on https://download.libsodium.org/doc/advanced/xchacha20.html and https://tools.ietf.org/html/draft-arciszewski-xchacha-02.
    ///
    /// This cipher is meant to be used to construct an AEAD with Poly1305.
    /// </summary>
    /// <seealso cref="NaCl.Core.Base.ChaCha20Base" />
    public class XChaCha20 : ChaCha20Base
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XChaCha20"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="initialCounter">The initial counter.</param>
        public XChaCha20(in byte[] key, int initialCounter) : base(key, initialCounter) { }

        /// <inheritdoc />
        protected override Array16<uint> CreateInitialState(ReadOnlySpan<byte> nonce, int counter)
        {
            if (nonce.IsEmpty || nonce.Length != NonceSizeInBytes())
                throw new CryptographicException(FormatNonceLengthExceptionMessage(GetType().Name, nonce.Length, NonceSizeInBytes()));

            // Set the initial state based on https://tools.ietf.org/html/draft-arciszewski-xchacha-01#section-2.3.
            var state = new Array16<uint>();

            // The first four words (0-3) are constants: 0x61707865, 0x3320646e, 0x79622d32, 0x6b206574.
            // The next eight words (4-11) are taken from the 256-bit key in little-endian order, in 4-byte chunks; and the first 16 bytes of the 24-byte nonce to obtain the subkey.
            SetSigma(ref state);
            SetKey(ref state, HChaCha20(nonce));

            // Word 12 is a block counter.
            state.x12 = (uint)counter;

            // Word 13 is a prefix of 4 null bytes, since RFC 8439 specifies a 12-byte nonce.
            state.x13 = 0;

            // Words 14-15 are the remaining 8-byte nonce (used in HChaCha20). Ref: https://tools.ietf.org/html/draft-arciszewski-xchacha-01#section-2.3.
            state.x14 = ByteIntegerConverter.LoadLittleEndian32(nonce, 16);
            state.x15 = ByteIntegerConverter.LoadLittleEndian32(nonce, 20);

            return state;
        }

        /// <summary>
        /// The size of the randomly generated nonces.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public override int NonceSizeInBytes() => 24;
    }
}
