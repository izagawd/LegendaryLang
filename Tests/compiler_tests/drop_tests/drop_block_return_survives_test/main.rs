use Std.Ops.Drop;
struct Dropper['a] {
    r: &'a uniq i32
}

impl['a] Drop for Dropper['a] {
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
