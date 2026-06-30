"""Tests for the Ghidra bridge plumbing that don't require Ghidra or a project.

The actual decompile/xref need a built FOTD project + a Ghidra install, so they
are exercised manually; here we cover the pure logic: sentinel extraction and
target disambiguation (symbol name vs prog:addr).
"""

import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import fomre  # noqa: E402
import ghidra  # noqa: E402
import symdb  # noqa: E402


class ExtractResultTests(unittest.TestCase):
    def test_extracts_from_noise(self):
        blob = ('INFO  Headless startup complete (HeadlessAnalyzer)\n'
                '@@FOMRE@@{"name": "Foo", "entry": "101a2390"}@@END@@\n'
                'INFO  REPORT: Import succeeded\n')
        out = ghidra.extract_result(blob)
        self.assertEqual(out["name"], "Foo")
        self.assertEqual(out["entry"], "101a2390")

    def test_no_sentinel_returns_none(self):
        self.assertIsNone(ghidra.extract_result("INFO just logs, no result\n"))


class ResolveTargetTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.db = symdb.SymbolDb().load()

    def test_prog_addr_form(self):
        prog, addr = fomre._resolve_target(self.db, "CShell.dll:0x1030dff0")
        self.assertEqual(prog, "CShell.dll")
        self.assertEqual(addr, 0x1030DFF0)

    def test_cpp_qualified_name_not_mistaken_for_prog_addr(self):
        # contains ':' but must resolve as a symbol, not prog:addr
        prog, addr = fomre._resolve_target(self.db, "FOM::Player::FillUpdate")
        self.assertEqual(prog, "CShell.dll")
        self.assertEqual(addr, 0x101A2390)


if __name__ == "__main__":
    unittest.main()
