use Std.Ops.Drop;
struct Dropper {
    r: &mut i32
}

impl Drop for Dropper {
    fn Drop(self: &uniq Self) {
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
