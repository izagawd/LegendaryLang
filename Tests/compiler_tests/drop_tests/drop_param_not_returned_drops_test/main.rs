use Std.Core.Marker.Drop;
struct Dropper {
    r: &mut i32
}

impl Drop for Dropper {
    fn Drop(self: &uniq Self) {
        *self.r = *self.r + 1;
    }
}

fn discard(d: Dropper) -> i32 {
    77
}

fn main() -> i32 {
    let counter = 0;
    let val = discard(make Dropper { r: &mut counter });
    counter
}
