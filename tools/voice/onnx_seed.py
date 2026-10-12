"""Gives a Piper (VITS) ONNX model's random nodes a fixed seed, so synthesis is repeatable.

Piper exports its sampling noise as ``RandomNormalLike`` nodes without a ``seed`` attribute, so onnxruntime draws
fresh noise on every run. Adding ``seed`` makes each new inference session draw the same noise, which keeps the
model's natural prosody (noise on) while every render stays byte-identical. Pure stdlib: the model's protobuf is
edited at the wire level (ModelProto.graph -> GraphProto.node -> NodeProto.attribute), no ``onnx`` package needed.
"""
import struct

_WIRE_VARINT = 0
_WIRE_FIXED64 = 1
_WIRE_BYTES = 2
_WIRE_FIXED32 = 5

_MODEL_GRAPH = 7
_GRAPH_NODE = 1
_NODE_OP_TYPE = 4
_NODE_ATTRIBUTE = 5
_ATTR_NAME = 1
_ATTR_FLOAT = 2
_ATTR_TYPE = 20
_ATTR_TYPE_FLOAT = 1

RANDOM_OPS = (b"RandomNormalLike", b"RandomNormal", b"RandomUniformLike", b"RandomUniform")


def _read_varint(data: bytes, i: int) -> tuple:
    value = shift = 0
    while True:
        byte = data[i]
        i += 1
        value |= (byte & 0x7F) << shift
        shift += 7
        if byte < 0x80:
            return value, i


def _varint(value: int) -> bytes:
    out = bytearray()
    while True:
        low = value & 0x7F
        value >>= 7
        if value:
            out.append(low | 0x80)
        else:
            out.append(low)
            return bytes(out)


def fields(data: bytes):
    """Yields ``(field, wire_type, payload_or_None, start, end)`` for each top-level field of a message."""
    i = 0
    while i < len(data):
        start = i
        key, i = _read_varint(data, i)
        field, wire = key >> 3, key & 7
        if wire == _WIRE_VARINT:
            _, i = _read_varint(data, i)
            yield field, wire, None, start, i
        elif wire == _WIRE_BYTES:
            length, i = _read_varint(data, i)
            yield field, wire, data[i:i + length], start, i + length
            i += length
        elif wire == _WIRE_FIXED32:
            i += 4
            yield field, wire, None, start, i
        elif wire == _WIRE_FIXED64:
            i += 8
            yield field, wire, None, start, i
        else:
            raise ValueError(f"unsupported protobuf wire type {wire} at byte {start}")


def _bytes_field(field: int, payload: bytes) -> bytes:
    return _varint(field << 3 | _WIRE_BYTES) + _varint(len(payload)) + payload


def _seed_attribute(seed: float) -> bytes:
    return (_bytes_field(_ATTR_NAME, b"seed") + _varint(_ATTR_FLOAT << 3 | _WIRE_FIXED32) + struct.pack("<f", seed)
            + _varint(_ATTR_TYPE << 3 | _WIRE_VARINT) + _varint(_ATTR_TYPE_FLOAT))


def _rewrite(message: bytes, target: int, transform) -> bytes:
    out = bytearray()
    for field, wire, payload, start, end in fields(message):
        if field == target and wire == _WIRE_BYTES:
            out += _bytes_field(field, transform(payload))
        else:
            out += message[start:end]
    return bytes(out)


def _attribute_names(node: bytes) -> list:
    names = []
    for field, _, payload, _, _ in fields(node):
        if field == _NODE_ATTRIBUTE:
            names.extend(p for f, _, p, _, _ in fields(payload) if f == _ATTR_NAME)
    return names


def _op_type(node: bytes) -> bytes:
    return next((p for f, _, p, _, _ in fields(node) if f == _NODE_OP_TYPE), b"")


def random_ops(model: bytes) -> list:
    """``(op_type, has_seed)`` for every random node in the model's main graph."""
    graph = next(p for f, _, p, _, _ in fields(model) if f == _MODEL_GRAPH)
    found = []
    for field, _, node, _, _ in fields(graph):
        if field == _GRAPH_NODE and _op_type(node) in RANDOM_OPS:
            found.append((_op_type(node).decode(), b"seed" in _attribute_names(node)))
    return found


def seed_model(model: bytes, seed: float) -> bytes:
    """The model with ``seed`` set on every random node of its main graph that has none."""
    def node(payload: bytes) -> bytes:
        if _op_type(payload) in RANDOM_OPS and b"seed" not in _attribute_names(payload):
            return payload + _bytes_field(_NODE_ATTRIBUTE, _seed_attribute(seed))
        return payload

    return _rewrite(model, _MODEL_GRAPH, lambda graph: _rewrite(graph, _GRAPH_NODE, node))
