use Std.Core.Marker.Drop;
struct Dropper {
    r: &uniq i32
}

impl Drop for Dropper {
    fn Drop(self: &uniq Self) {
        *self.r = *self.r + 1;
    }
}

fn main() -> i32 {
    let counter = 0;
    let val: i32 = {
        let d = make Dropper { r: &uniq counter };
        42
    };
    val + counter
}
