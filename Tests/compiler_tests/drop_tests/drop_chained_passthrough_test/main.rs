use Std.Ops.Drop;
struct Dropper['a] {
    r: &'a mut i32
}

impl['a] Drop for Dropper['a] {
    fn Drop(self: &mut Self) {
        *self.r = *self.r + 1;
    }
}

fn pass1(d: Dropper) -> Dropper { d }
fn pass2(d: Dropper) -> Dropper { d }
fn pass3(d: Dropper) -> Dropper { d }

fn consume(d: Dropper) {}

fn main() -> i32 {
    let counter = 0;
    let d = pass3(pass2(pass1(make Dropper { r: &mut counter })));
    consume(d);
    counter
}
