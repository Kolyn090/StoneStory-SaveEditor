import json
import re


def skip_ws(s: str, i: int) -> int:
    while i < len(s) and s[i] in " \t\r\n\u2002":
        i += 1
    return i


def parse_slim_value(s: str, i: int = 0):
    i = skip_ws(s, i)

    if i >= len(s):
        return None, i

    if s[i] == "{":
        return parse_slim_object(s, i)

    if s[i] == "[":
        return parse_slim_array(s, i)

    if s[i] == '"':
        return parse_slim_quoted_string(s, i)

    return parse_slim_bare_value(s, i)


def parse_slim_object(s: str, i: int):
    assert s[i] == "{"
    i += 1
    obj = {}

    while True:
        i = skip_ws(s, i)

        if i >= len(s):
            raise ValueError("Unclosed object")

        if s[i] == "}":
            return obj, i + 1

        key, i = parse_slim_key(s, i)

        i = skip_ws(s, i)

        if i >= len(s) or s[i] != ":":
            raise ValueError(f"Expected ':' after key {key!r} at index {i}")

        i += 1

        value, i = parse_slim_value(s, i)
        obj[key] = value

        i = skip_ws(s, i)

        if i < len(s) and s[i] == ",":
            i += 1
            continue

        if i < len(s) and s[i] == "}":
            return obj, i + 1

        raise ValueError(f"Expected ',' or '}}' at index {i}")


def parse_slim_array(s: str, i: int):
    assert s[i] == "["
    i += 1
    arr = []

    while True:
        i = skip_ws(s, i)

        if i >= len(s):
            raise ValueError("Unclosed array")

        if s[i] == "]":
            return arr, i + 1

        value, i = parse_slim_value(s, i)
        arr.append(value)

        i = skip_ws(s, i)

        if i < len(s) and s[i] == ",":
            i += 1
            continue

        if i < len(s) and s[i] == "]":
            return arr, i + 1

        raise ValueError(f"Expected ',' or ']' at index {i}")


def parse_slim_key(s: str, i: int):
    i = skip_ws(s, i)

    if s[i] == '"':
        return parse_slim_quoted_string(s, i)

    start = i

    while i < len(s) and s[i] not in ": \t\r\n":
        i += 1

    return s[start:i], i


def parse_slim_quoted_string(s: str, i: int):
    assert s[i] == '"'
    i += 1
    out = []

    while i < len(s):
        c = s[i]

        if c == "\\" and i + 1 < len(s):
            nxt = s[i + 1]

            # Keep common escapes readable
            if nxt == "n":
                out.append("\n")
            elif nxt == "t":
                out.append("\t")
            elif nxt == "r":
                out.append("\r")
            else:
                # SlimJson often uses escaping like \, \: \{ \}
                # In those cases, keep the escaped char itself.
                out.append(nxt)

            i += 2
            continue

        if c == '"':
            return "".join(out), i + 1

        out.append(c)
        i += 1

    raise ValueError("Unclosed quoted string")


def parse_slim_bare_value(s: str, i: int):
    start = i
    escaped = False

    while i < len(s):
        c = s[i]

        if escaped:
            escaped = False
            i += 1
            continue

        if c == "\\":
            escaped = True
            i += 1
            continue

        if c in ",}]":
            break

        i += 1

    raw = s[start:i].strip()
    return convert_slim_scalar(unescape_slim(raw)), i


def unescape_slim(value: str) -> str:
    # SlimJson escapes dictionary special chars like \, \: \{ \}
    out = []
    escaped = False

    for c in value:
        if escaped:
            out.append(c)
            escaped = False
        elif c == "\\":
            escaped = True
        else:
            out.append(c)

    if escaped:
        out.append("\\")

    return "".join(out)


def convert_slim_scalar(value: str):
    v = value.strip()

    if v == "":
        return ""

    if v == "null":
        return None

    if v.lower() == "true":
        return True

    if v.lower() == "false":
        return False

    # Int
    if re.fullmatch(r"-?\d+", v):
        try:
            return int(v)
        except ValueError:
            pass

    # Float, including scientific notation like 3.814697E-05
    if re.fullmatch(r"-?(?:\d+\.\d*|\d*\.\d+)(?:[eE][+-]?\d+)?", v) or re.fullmatch(r"-?\d+[eE][+-]?\d+", v):
        try:
            return float(v)
        except ValueError:
            pass

    # Keep things like 4.27.2 as string
    return v


def slim_to_python(s: str):
    value, i = parse_slim_value(s, 0)
    i = skip_ws(s, i)

    if i != len(s):
        raise ValueError(f"Extra data after index {i}: {s[i:i+80]!r}")

    return value


def convert_sections_json(input_path: str, output_path: str):
    with open(input_path, "r", encoding="utf-8") as f:
        top = json.load(f)

    converted = {}

    for key, value in top.items():
        if isinstance(value, str):
            stripped = value.strip()

            if stripped.startswith("{") or stripped.startswith("["):
                try:
                    converted[key] = slim_to_python(stripped)
                except Exception as e:
                    print(f"[WARN] Could not parse section {key!r}: {e}")
                    converted[key] = value
            else:
                converted[key] = convert_slim_scalar(value)
        else:
            converted[key] = value

    with open(output_path, "w", encoding="utf-8") as f:
        json.dump(converted, f, indent=2, ensure_ascii=False)

    print(f"Saved readable JSON to {output_path}")


def slim_parse(sjson: str, key: str) -> str | None:
    search = key + ":"
    length = len(search)

    bracket_stack = []
    prev = "."

    found = -1

    for i in range(0, len(sjson) - length):
        c = sjson[i]

        # Match only top-level keys:
        # bracket_stack count == 1 means we are inside the root {...}
        if (
            c == search[0]
            and len(bracket_stack) == 1
            and prev in (" ", "\u2002", ",", "{", "\t", "\r", "\n")
        ):
            if sjson[i:i + length] == search:
                found = i
                break

        elif c == "{" or c == "[":
            bracket_stack.append(c)

        elif bracket_stack and (
            (c == "}" and bracket_stack[-1] == "{")
            or (c == "]" and bracket_stack[-1] == "[")
        ):
            bracket_stack.pop()

        prev = c

    if found < 0:
        return None

    start = found + length

    if start >= len(sjson):
        return None

    first = sjson[start]

    # Object or array value
    if first in "{[":
        bracket_stack = [first]
        current = first

        for k in range(start + 1, len(sjson)):
            ch = sjson[k]

            if ch == '"':
                if current == '"':
                    current = bracket_stack[-1]
                else:
                    current = '"'

            if current != '"':
                if (ch == "}" and current == "{") or (ch == "]" and current == "["):
                    bracket_stack.pop()

                    if not bracket_stack:
                        return sjson[start:k + 1]

                    current = bracket_stack[-1]

                elif ch == "{" or ch == "[":
                    bracket_stack.append(ch)
                    current = ch

        return None

    # Quoted string value
    if first == '"':
        end = sjson.find('"', start + 1)
        if end < 0:
            return None
        return sjson[start + 1:end]

    # Bare value
    for k in range(start + 1, len(sjson)):
        if sjson[k] in ",}]":
            return sjson[start:k]

    return None


def slim_parse_int(sjson: str, key: str, default: int = 0) -> int:
    value = slim_parse(sjson, key)
    if value is None or value == "null":
        return default
    return int(value)


def slim_parse_bool(sjson: str, key: str, default: bool = False) -> bool:
    value = slim_parse(sjson, key)
    if value is None:
        return default
    return value.lower() == "true"


if __name__ == "__main__":
    convert_sections_json(
        "../data/decrypted_save_sections.json",
        "../data/decrypted_save_readable.json",
    )
