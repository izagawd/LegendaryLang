use Std.Core.Marker.Drop;
struct Dropper {
    r: &mut i32
}

impl Drop for Dropper {
    fn Drop(self: &uniq Self) {
        *self.r = *self.r + 10;
    }
}

fn passthrough(d: Dropper) -> Dropper {
    d
}

fn consume(d: Dropper) {}

fn main() -> i32 {
    let counter = 0;
    let d = make Dropper { r: &mut counter };
    let d2 = passthrough(d);
    consume(d2);
    counter
}
