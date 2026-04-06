use Std.Core.Marker.Drop;
struct Dropper {
    r: &uniq i32
}

impl Drop for Dropper {
    fn Drop(self: &uniq Self) {
        *self.r = *self.r + 1;
    }
}

struct Multi {
    a: Dropper,
    plain: i32,
    b: Dropper
}

fn main() -> i32 {
    let counter = 0;
    {
        let m = make Multi {
            a: make Dropper { r: &uniq counter },
            plain: 99,
            b: make Dropper { r: &uniq counter }
        };
    }
    counter
}
