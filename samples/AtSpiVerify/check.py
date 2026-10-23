#!/usr/bin/env python3
"""Reads AtSpiVerify over AT-SPI as a screen reader would and prints what it found as JSON.

Usage: check.py <trigger file> <output file>. The app adds a third row when the trigger file appears.
"""
import json
import pathlib
import sys
import time

import gi

gi.require_version("Atspi", "2.0")
from gi.repository import Atspi, GLib  # noqa: E402

trigger = pathlib.Path(sys.argv[1])
output = pathlib.Path(sys.argv[2])
events = {"children-changed": [], "name-changes": []}


def on_event(event):
    try:
        source = event.source.get_name() or ""
    except GLib.Error:
        source = "?"
    if event.type.startswith("object:children-changed"):
        events["children-changed"].append(f"{event.type} index={event.detail1} on '{source}'")
    else:
        events["name-changes"].append(source)


listener = Atspi.EventListener.new(on_event)
listener.register("object:children-changed")
listener.register("object:property-change:accessible-name")


def pump(seconds):
    context = GLib.MainContext.default()
    end = time.monotonic() + seconds
    while time.monotonic() < end:
        context.iteration(False)
        time.sleep(0.02)


def children(element):
    try:
        return [element.get_child_at_index(i) for i in range(element.get_child_count())]
    except GLib.Error:
        return []


def descendants(element, depth=0):
    yield element
    if depth < 40:
        for child in children(element):
            if child is not None:
                yield from descendants(child, depth + 1)


def frame():
    for app in children(Atspi.get_desktop(0)):
        for window in children(app):
            if window is not None and window.get_name() == "AtSpiVerify":
                return window
    return None


window = None
for _ in range(180):
    window = frame()
    if window is not None:
        break
    pump(0.5)
if window is None:
    output.write_text(json.dumps({"error": "no AtSpiVerify window on the accessibility bus"}))
    sys.exit(1)

found = list(descendants(window))
listing = next(e for e in found if e.get_role() in (Atspi.Role.LIST, Atspi.Role.LIST_BOX))
labels = [e.get_name() for e in found if e.get_role() == Atspi.Role.LABEL]


def rows():
    listing.clear_cache()
    return [child.get_name() for child in children(listing)]


before = rows()
events["name-changes"].clear()
trigger.touch()
pump(3)
after = rows()

output.write_text(json.dumps({
    "rows_before": before,
    "rows_after": after,
    "labels": labels,
    "children_changed_events": events["children-changed"],
    "name_change_events": events["name-changes"],
}, indent=2))
