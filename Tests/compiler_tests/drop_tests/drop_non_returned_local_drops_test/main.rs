use Std.Ops.Drop;
struct Dropper['a] {
    r: &'a mut i32
}

impl['a] Drop for Dropper['a] {
    fn Drop(self: &uniq Self) {
        *self.r = *self.r + 1;
    }
}

fn make_and_discard(r: &mut i32) -> i32 {
    let d = make Dropper { r: r };
    42
}

fn main() -> i32 {
    let counter = 0;
    let val = make_and_discard(&mut counter);
    val + counter
}
