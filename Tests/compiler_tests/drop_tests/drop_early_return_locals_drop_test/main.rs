use Std.Ops.Drop;
struct Dropper {
    r: &mut i32
}

impl Drop for Dropper {
    fn Drop(self: &uniq Self) {
        *self.r = *self.r + 1;
    }
}

fn early_out(r1: &mut i32, r2: &mut i32) -> Dropper {
    let local = make Dropper { r: r1 };
    return make Dropper { r: r2 };
}

fn consume(d: Dropper) {}

fn main() -> i32 {
    let local_counter = 0;
    let return_counter = 0;
    let d = early_out(&mut local_counter, &mut return_counter);
    consume(d);
    local_counter + return_counter
}
