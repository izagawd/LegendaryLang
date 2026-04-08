use Std.Ops.Drop;
struct Dropper['a] {
    r: &'a mut i32
}

impl['a] Drop for Dropper['a] {
    fn Drop(self: &uniq Self) {
        *self.r = *self.r + 1;
    }
}

fn consume(d: Dropper) {}

fn main() -> i32 {
    let counter = 0;
    let d: Dropper = {
        {
            make Dropper { r: &mut counter }
        }
    };
    consume(d);
    counter
}
