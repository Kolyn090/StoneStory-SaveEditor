import json
from slim_json_parser import slim_parse
from string_cipher import decrypt


if __name__ == "__main__":
    with open("../data/encrypted_progress.txt", "r", encoding="utf-8") as f:
        encrypted = f.readline()

    # encrypted = encrypt(original)
    decrypted = decrypt(encrypted)

    with open("../data/decrypted_progress.txt", "w", encoding="utf-8") as f:
        f.write(decrypted)

    sections = [
        "version",
        "rng",
        "hero_settings",
        "progress_flags",
        "quest_data",
        "inventory_data",
        "cosmetics",
        "treasure_factory",
        "foe_book",
        "ui_state",
        "shop_states",
        "crypt_intro",
        "xp",
        "ouroboros",
        "utility_belt",
        "craft_book",
        "achievements",
        "mutator",
        "events",
        "custom_quests",
        "weekly_quest",
        "goals",
        "subs",
        "prom",
        "leaderboards",
        "talents",
        "mind_stone",
    ]

    out = {}

    for section in sections:
        value = slim_parse(decrypted, section)
        if value is not None:
            out[section] = value

    with open("../data/decrypted_save_sections.json", "w", encoding="utf-8") as f:
        json.dump(out, f, indent=2, ensure_ascii=False)
