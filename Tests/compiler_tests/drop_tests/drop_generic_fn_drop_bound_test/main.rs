use Std.Ops.Drop;
struct Dropper['a] {
    r: &'a uniq i32
}

impl['a] Drop for Dropper['a] {
    fn Drop(self: &uniq Self) {
        *self.r = *self.r + 1;
    }
}

fn take_and_drop(T:! Drop, val: T) -> i32 {
    0
}

fn main() -> i32 {
    let counter = 0;
    take_and_drop(Dropper, make Dropper { r : &uniq counter });
    counter
}
