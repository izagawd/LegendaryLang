use Std.Ops.Drop;
struct Dropper['a] {
    r: &'a mut i32
}

impl['a] Drop for Dropper['a] {
    fn Drop(self: &uniq Self) {
        *self.r = *self.r + 10;
    }
}

fn passthrough(d: Dropper) -> Dropper {
    return d;
}

fn consume(d: Dropper) {}

fn main() -> i32 {
    let counter = 0;
    let d = make Dropper { r: &mut counter };
    let d2 = passthrough(d);
    consume(d2);
    counter
}
