"""
tools/moonbatch/slots.py record writing: a record swap that Windows refuses for a moment must not fail the batch.
Run: python -m unittest discover -s tools/tests -t tools
"""
import json
import os
import tempfile
import unittest
from pathlib import Path
from unittest import mock

from moonbatch import slots


class WriteJsonTests(unittest.TestCase):
    def setUp(self):
        self._dir = tempfile.TemporaryDirectory()
        self.path = str(Path(self._dir.name) / "slot-1.json")
        self._real_replace = os.replace
        self._sleep = mock.patch.object(slots.time, "sleep")
        self._sleep.start()

    def tearDown(self):
        self._sleep.stop()
        self._dir.cleanup()

    def _flaky_replace(self, failures):
        calls = {"n": 0}

        def replace(src, dst):
            calls["n"] += 1
            if calls["n"] <= failures:
                raise PermissionError(5, "Access is denied")
            self._real_replace(src, dst)

        return replace, calls

    def test_a_swap_refused_a_few_times_is_retried_until_it_lands(self):
        replace, calls = self._flaky_replace(3)
        with mock.patch.object(slots.os, "replace", replace):
            slots._write_json(self.path, {"pid": 7})
        self.assertEqual(calls["n"], 4)
        self.assertEqual(slots._read_json(self.path), {"pid": 7})
        self.assertEqual(os.listdir(self._dir.name), ["slot-1.json"])

    def test_a_swap_that_never_lands_writes_in_place_and_leaves_no_temp_file(self):
        replace, calls = self._flaky_replace(10 ** 6)
        with mock.patch.object(slots.os, "replace", replace):
            slots._write_json(self.path, {"pid": 9})
        self.assertEqual(calls["n"], slots.REPLACE_ATTEMPTS)
        with open(self.path, encoding="utf-8") as f:
            self.assertEqual(json.load(f), {"pid": 9})
        self.assertEqual(os.listdir(self._dir.name), ["slot-1.json"])


if __name__ == "__main__":
    unittest.main()
