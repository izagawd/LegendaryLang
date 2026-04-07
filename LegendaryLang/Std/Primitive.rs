use Std.Marker.Primitive;
use Std.Ops.TryInto;

fn TryCastPrimitive[From:! Primitive](To:! Primitive, input: From) -> Option(To);

impl TryInto(u8) for i32 {
    fn try_into(self: i32) -> Option(u8) {
        TryCastPrimitive(u8, self)
    }
}

impl TryInto(usize) for i32 {
    fn try_into(self: i32) -> Option(usize) {
        TryCastPrimitive(usize, self)
    }
}

impl TryInto(i32) for u8 {
    fn try_into(self: u8) -> Option(i32) {
        TryCastPrimitive(i32, self)
    }
}

impl TryInto(usize) for u8 {
    fn try_into(self: u8) -> Option(usize) {
        TryCastPrimitive(usize, self)
    }
}

impl TryInto(i32) for usize {
    fn try_into(self: usize) -> Option(i32) {
        TryCastPrimitive(i32, self)
    }
}

impl TryInto(u8) for usize {
    fn try_into(self: usize) -> Option(u8) {
        TryCastPrimitive(u8, self)
    }
}
