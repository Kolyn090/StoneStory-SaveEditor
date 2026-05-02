from string_cipher import encrypt

if __name__ == "__main__":
    with open("../data/decrypted_progress.txt", "r", encoding="utf-8") as f:
        content = f.readline()
    
    encrypted = encrypt(content)

    with open("../data/encrypted_progress2.txt", "w", encoding="utf-8") as f:
        f.write(encrypted)
