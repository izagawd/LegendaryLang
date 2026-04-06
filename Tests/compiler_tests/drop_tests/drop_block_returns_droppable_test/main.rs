use Std.Core.Marker.Drop;
struct Dropper {
    r: &mut i32
}

impl Drop for Dropper {
    fn Drop(self: &uniq Self) {
        *self.r = *self.r + 1;
    }
}

fn consume(d: Dropper) {}

fn main() -> i32 {
    let counter = 0;
    let d: Dropper = {
        make Dropper { r: &mut counter }
    };
    consume(d);
    counter
}
