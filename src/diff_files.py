import difflib
import sys
from pathlib import Path


def compare_files(file1, file2):
    path1 = Path(file1)
    path2 = Path(file2)

    if not path1.exists():
        print(f"Error: {file1} does not exist")
        return

    if not path2.exists():
        print(f"Error: {file2} does not exist")
        return

    with path1.open("r", encoding="utf-8", errors="replace") as f1:
        lines1 = f1.readlines()

    with path2.open("r", encoding="utf-8", errors="replace") as f2:
        lines2 = f2.readlines()

    diff = difflib.unified_diff(
        lines1,
        lines2,
        fromfile=str(path1),
        tofile=str(path2),
        lineterm=""
    )

    for line in diff:
        print(line[:100])


if __name__ == "__main__":
    if len(sys.argv) != 3:
        print("Usage: python diff_files.py <file1> <file2>")
        sys.exit(1)

    compare_files(sys.argv[1], sys.argv[2])
