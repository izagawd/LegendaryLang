use Std.Ops.Drop;
struct Dropper['a] {
    r: &'a uniq i32
}

impl['a] Drop for Dropper['a] {
    fn Drop(self: &uniq Self) {
        *self.r = *self.r + 100;
    }
}

fn make_and_return['a](r: &'a uniq i32) -> i32 {
    let d = make Dropper { r: r };
    42
}

fn main() -> i32 {
    let counter = 0;
    let val: i32 = make_and_return(&uniq counter);
    val
}
