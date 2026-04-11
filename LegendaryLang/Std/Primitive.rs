use Std.Marker.Primitive;
use Std.Ops.TryInto;
use Std.Primitive.TryCastPrimitive;

fn TryCastPrimitive[From:! Sized +Primitive](To:! Sized +Primitive, input: From) -> Option(To);

impl TryInto(u8) for i32 {
    fn TryInto(self: i32) -> Option(u8) {
        TryCastPrimitive(u8, self)
    }
}

impl TryInto(usize) for i32 {
    fn TryInto(self: i32) -> Option(usize) {
        TryCastPrimitive(usize, self)
    }
}

impl TryInto(i32) for u8 {
    fn TryInto(self: u8) -> Option(i32) {
        TryCastPrimitive(i32, self)
    }
}

impl TryInto(usize) for u8 {
    fn TryInto(self: u8) -> Option(usize) {
        TryCastPrimitive(usize, self)
    }
}

impl TryInto(i32) for usize {
    fn TryInto(self: usize) -> Option(i32) {
        TryCastPrimitive(i32, self)
    }
}

impl TryInto(u8) for usize {
    fn TryInto(self: usize) -> Option(u8) {
        TryCastPrimitive(u8, self)
    }
}
