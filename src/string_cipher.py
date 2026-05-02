import base64
import hashlib
import json
import os
from typing import Union

from pprp.crypto import rijndael


KEY_SIZE = 32          # 256-bit key
BLOCK_SIZE = 32        # 256-bit Rijndael block size
ITERATIONS = 1000

DEFAULT_PASSPHRASE = "peekabeyoufoundme"


def _to_bytes(value: Union[str, bytes]) -> bytes:
    """
    pprp sometimes returns a Python str containing byte-values 0..255.
    Convert that safely back to real bytes.
    """
    if isinstance(value, bytes):
        return value
    return value.encode("latin-1")


def pkcs7_pad(data: bytes, block_size: int = BLOCK_SIZE) -> bytes:
    pad_len = block_size - (len(data) % block_size)
    return data + bytes([pad_len]) * pad_len


def pkcs7_unpad(data: bytes, block_size: int = BLOCK_SIZE) -> bytes:
    if not data:
        raise ValueError("Empty data cannot be unpadded")

    pad_len = data[-1]

    if pad_len < 1 or pad_len > block_size:
        raise ValueError(f"Invalid PKCS7 padding length: {pad_len}")

    expected = bytes([pad_len]) * pad_len
    if data[-pad_len:] != expected:
        raise ValueError("Invalid PKCS7 padding bytes")

    return data[:-pad_len]


def derive_key(passphrase: str, salt: bytes) -> bytes:
    """
    Matches old .NET Rfc2898DeriveBytes(passPhrase, salt, 1000).GetBytes(32)

    Old Rfc2898DeriveBytes default = PBKDF2-HMAC-SHA1.
    """
    return hashlib.pbkdf2_hmac(
        "sha1",
        passphrase.encode("utf-8"),
        salt,
        ITERATIONS,
        dklen=KEY_SIZE,
    )


def xor_bytes(a: bytes, b: bytes) -> bytes:
    return bytes(x ^ y for x, y in zip(a, b))


def encrypt_block(cipher: rijndael, block: bytes) -> bytes:
    encrypted = cipher.encrypt(block)
    return _to_bytes(encrypted)


def decrypt_block(cipher: rijndael, block: bytes) -> bytes:
    decrypted = cipher.decrypt(block)
    return _to_bytes(decrypted)


def encrypt(plain_text: str, passphrase: str = DEFAULT_PASSPHRASE) -> str:
    salt = os.urandom(32)
    iv = os.urandom(32)

    key = derive_key(passphrase, salt)

    cipher = rijndael(key, block_size=BLOCK_SIZE)

    plain_bytes = plain_text.encode("utf-8")
    padded = pkcs7_pad(plain_bytes, BLOCK_SIZE)

    encrypted_blocks = []
    previous = iv

    # Manual CBC mode:
    # encrypted_block = Rijndael(plain_block XOR previous_cipher_block)
    for i in range(0, len(padded), BLOCK_SIZE):
        block = padded[i:i + BLOCK_SIZE]
        mixed = xor_bytes(block, previous)
        encrypted = encrypt_block(cipher, mixed)

        encrypted_blocks.append(encrypted)
        previous = encrypted

    ciphertext = b"".join(encrypted_blocks)

    # C# format:
    # Base64( salt[32] + iv[32] + ciphertext )
    packed = salt + iv + ciphertext
    return base64.b64encode(packed).decode("ascii")


def decrypt(cipher_text: str, passphrase: str = DEFAULT_PASSPHRASE) -> str:
    raw = base64.b64decode(cipher_text)

    if len(raw) < 64:
        raise ValueError("Cipher text is too short")

    salt = raw[:32]
    iv = raw[32:64]
    ciphertext = raw[64:]

    if len(ciphertext) % BLOCK_SIZE != 0:
        raise ValueError("Ciphertext length is not a multiple of 32 bytes")

    key = derive_key(passphrase, salt)

    cipher = rijndael(key, block_size=BLOCK_SIZE)

    plain_blocks = []
    previous = iv

    # Manual CBC mode:
    # plain_block = RijndaelDecrypt(cipher_block) XOR previous_cipher_block
    for i in range(0, len(ciphertext), BLOCK_SIZE):
        block = ciphertext[i:i + BLOCK_SIZE]

        decrypted = decrypt_block(cipher, block)
        plain = xor_bytes(decrypted, previous)

        plain_blocks.append(plain)
        previous = block

    padded_plain = b"".join(plain_blocks)
    plain = pkcs7_unpad(padded_plain, BLOCK_SIZE)

    return plain.decode("utf-8")
