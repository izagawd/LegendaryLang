use Std.Ops.Drop;
struct Dropper['a] {
    r: &'a mut i32
}

impl['a] Drop for Dropper['a] {
    fn Drop(self: &mut Self) {
        *self.r = *self.r + 1;
    }
}

fn take_and_drop(T:! Drop, val: T) -> i32 {
    0
}

fn main() -> i32 {
    let counter = 0;
    take_and_drop(Dropper, make Dropper { r : &mut counter });
    counter
}
